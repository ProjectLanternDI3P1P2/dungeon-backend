using System.Net.Http.Json;
using Dungeon.Application.Features.DungeonRunUseCase.GetDungeonRunById;
using Dungeon.Application.Features.DungeonUseCase.GetDungeonMap;
using Dungeon.Presentation.DTO;
using Dungeon.Presentation.Hubs;
using FluentAssertions;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.AspNetCore.TestHost;

namespace Dungeon.Test.Integration.Dungeons;

/// <summary>Gameplay of a run through the SignalR Hub, over WebSocket and PostgreSQL.</summary>
public sealed class DungeonHubIntegrationTests : IAsyncLifetime
{
    private TestDatabase? database;
    private DungeonWebApplicationFactory? factory;
    private HubConnection? connection;

    private HubConnection Connection =>
        connection ?? throw new InvalidOperationException("Test not initialized.");

    public async ValueTask InitializeAsync()
    {
        database = await TestDatabase.CreateAsync($"dungeon_test_hub_{Guid.NewGuid():N}");
        factory = new DungeonWebApplicationFactory(database.ConnectionString);
        TestServer server = factory.Server;
        connection = new HubConnectionBuilder()
            .WithUrl(
                new Uri(server.BaseAddress, DungeonHub.Route),
                options =>
                {
                    options.HttpMessageHandlerFactory = _ => server.CreateHandler();
                    options.WebSocketFactory = async (context, cancellationToken) =>
                        await server
                            .CreateWebSocketClient()
                            .ConnectAsync(context.Uri, cancellationToken);
                }
            )
            .Build();
        await connection.StartAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task MoveHero_TowardsAFloorTile_AcknowledgesAndBroadcastsTheNewSnapshot()
    {
        // Arrange
        GetDungeonRunByIdResult run = await CreateRunAsync();
        (string direction, int dx, int dy) = await FindFloorStepAsync(run);
        var broadcast = new TaskCompletionSource<GetDungeonRunByIdResult>();
        Connection.On<GetDungeonRunByIdResult>(
            nameof(IDungeonClient.DungeonStateChanged),
            snapshot => broadcast.TrySetResult(snapshot)
        );
        await JoinAsync(run.Id);

        // Act
        DungeonCommandResult result = await MoveAsync(Guid.NewGuid(), run.Id, direction);

        // Assert
        result.Accepted.Should().BeTrue();
        result.Snapshot!.Hero.X.Should().Be(run.Hero.X + dx);
        result.Snapshot.Hero.Y.Should().Be(run.Hero.Y + dy);
        result.Snapshot.Turn.Should().Be(1);
        GetDungeonRunByIdResult received = await broadcast.Task.WaitAsync(
            TimeSpan.FromSeconds(5),
            TestContext.Current.CancellationToken
        );
        received.Turn.Should().Be(1);
    }

    [Fact]
    public async Task MoveHero_SameCommandSentTwice_MovesTheHeroOnce()
    {
        // Arrange
        GetDungeonRunByIdResult run = await CreateRunAsync();
        (string direction, int dx, int dy) = await FindFloorStepAsync(run);
        Guid commandId = Guid.NewGuid();

        // Act
        await MoveAsync(commandId, run.Id, direction);
        DungeonCommandResult retried = await MoveAsync(commandId, run.Id, direction);
        DungeonCommandResult current = await JoinAsync(run.Id);

        // Assert
        retried.Accepted.Should().BeTrue();
        current.Snapshot!.Turn.Should().Be(1);
        current.Snapshot.Hero.X.Should().Be(run.Hero.X + dx);
        current.Snapshot.Hero.Y.Should().Be(run.Hero.Y + dy);
    }

    [Fact]
    public async Task JoinDungeon_AfterAMove_ReturnsTheTilesTheHeroStoodOn()
    {
        // Arrange
        GetDungeonRunByIdResult run = await CreateRunAsync();
        (string direction, int dx, int dy) = await FindFloorStepAsync(run);
        int width = (await GetMapAsync(run)).Width;
        await MoveAsync(Guid.NewGuid(), run.Id, direction);

        // Act: what a reloaded page gets back, read from the database.
        DungeonCommandResult result = await JoinAsync(run.Id);

        // Assert
        byte[] tiles = Convert.FromBase64String(result.Snapshot!.VisitedTiles);
        bool Visited(int x, int y) =>
            (tiles[(y * width + x) / 8] & (1 << ((y * width + x) % 8))) != 0;
        Visited(run.Hero.X, run.Hero.Y).Should().BeTrue();
        Visited(run.Hero.X + dx, run.Hero.Y + dy).Should().BeTrue();
        tiles.Sum(b => System.Numerics.BitOperations.PopCount(b)).Should().Be(2);
        result.Snapshot.DiscoveredRoomIds.Should().Equal(run.DiscoveredRoomIds);
    }

    [Fact]
    public async Task MoveHero_UnknownDirection_IsRefusedAsInvalid()
    {
        // Arrange
        GetDungeonRunByIdResult run = await CreateRunAsync();

        // Act
        DungeonCommandResult result = await MoveAsync(Guid.NewGuid(), run.Id, "up");

        // Assert
        result.Accepted.Should().BeFalse();
        result.Error!.Code.Should().Be(DungeonCommandErrorCodes.Validation);
    }

    [Fact]
    public async Task JoinDungeon_UnknownRun_IsRefusedAsNotFound()
    {
        // Act
        DungeonCommandResult result = await JoinAsync(Guid.NewGuid());

        // Assert
        result.Accepted.Should().BeFalse();
        result.Error!.Code.Should().Be(DungeonCommandErrorCodes.NotFound);
    }

    public async ValueTask DisposeAsync()
    {
        if (connection is not null)
            await connection.DisposeAsync();
        factory?.Dispose();
        if (database is not null)
            await database.DisposeAsync();
    }

    private async Task<GetDungeonRunByIdResult> CreateRunAsync()
    {
        using HttpClient client = factory!.CreateClient();
        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/v1/dungeon-runs",
            new CreateDungeonRunDto { GameSessionId = Guid.NewGuid() },
            TestContext.Current.CancellationToken
        );
        return (
            await response.Content.ReadFromJsonAsync<GetDungeonRunByIdResult>(
                TestContext.Current.CancellationToken
            )
        )!;
    }

    /// <summary>
    /// The seed is random, and so is the start room: a column may stand beside the entrance.
    /// The party steps onto whichever neighbouring tile is plain floor.
    /// </summary>
    private async Task<(string Direction, int Dx, int Dy)> FindFloorStepAsync(
        GetDungeonRunByIdResult run
    )
    {
        GetDungeonMapResult map = await GetMapAsync(run);
        return new[] { ("east", 1, 0), ("west", -1, 0), ("south", 0, 1) }.First(move =>
            map.Rows[run.Hero.Y + move.Item3][run.Hero.X + move.Item2] == '.'
        );
    }

    private async Task<GetDungeonMapResult> GetMapAsync(GetDungeonRunByIdResult run)
    {
        using HttpClient client = factory!.CreateClient();
        return (
            await client.GetFromJsonAsync<GetDungeonMapResult>(
                $"/api/v1/dungeons/{run.Seed}/map",
                TestContext.Current.CancellationToken
            )
        )!;
    }

    private Task<DungeonCommandResult> JoinAsync(Guid dungeonId) =>
        Connection.InvokeAsync<DungeonCommandResult>(
            nameof(DungeonHub.JoinDungeon),
            new JoinDungeonRequest(dungeonId),
            TestContext.Current.CancellationToken
        );

    private Task<DungeonCommandResult> MoveAsync(
        Guid commandId,
        Guid dungeonId,
        string direction
    ) =>
        Connection.InvokeAsync<DungeonCommandResult>(
            nameof(DungeonHub.MoveHero),
            new MoveHeroRequest(commandId, dungeonId, direction),
            TestContext.Current.CancellationToken
        );
}
