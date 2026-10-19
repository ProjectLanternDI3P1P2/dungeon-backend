using System.Net;
using System.Net.Http.Json;
using Dungeon.Application.Features.DungeonRunUseCase.GetDungeonRunById;
using Dungeon.Contracts.V1;
using FluentAssertions;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.AspNetCore.TestHost;

namespace Dungeon.Test.Integration.Dungeons;

/// <summary>Player starts the run of a game session through the internal gRPC contract.</summary>
public sealed class DungeonRunGrpcServiceIntegrationTests : IAsyncLifetime
{
    private TestDatabase? database;
    private DungeonWebApplicationFactory? factory;
    private GrpcChannel? channel;
    private DungeonRunService.DungeonRunServiceClient? client;

    public async ValueTask InitializeAsync()
    {
        database = await TestDatabase.CreateAsync($"dungeon_test_run_grpc_{Guid.NewGuid():N}");
        factory = new DungeonWebApplicationFactory(database.ConnectionString);
        TestServer server = factory.Server;
        channel = GrpcChannel.ForAddress(
            "http://localhost",
            new GrpcChannelOptions { HttpHandler = server.CreateHandler() }
        );
        client = new DungeonRunService.DungeonRunServiceClient(channel);
    }

    [Fact]
    public async Task CreateDungeonRun_NewSession_StartsARunReadableThroughRest()
    {
        // Arrange
        CreateDungeonRunRequest request = Request(Guid.NewGuid(), Guid.NewGuid());

        // Act
        CreateDungeonRunResponse response = await Client.CreateDungeonRunAsync(
            request,
            cancellationToken: TestContext.Current.CancellationToken
        );
        HttpResponseMessage rest = await factory!
            .CreateClient()
            .GetAsync(
                $"/api/v1/dungeon-runs/{response.RunId}",
                TestContext.Current.CancellationToken
            );
        var run = await rest.Content.ReadFromJsonAsync<GetDungeonRunByIdResult>(
            TestContext.Current.CancellationToken
        );

        // Assert
        response.RunId.Should().Be(request.CommandId);
        response.Seed.Should().HaveLength(13);
        rest.StatusCode.Should().Be(HttpStatusCode.OK);
        run!.GameSessionId.Should().Be(Guid.Parse(request.GameSessionId));
        run.Seed.Should().Be(response.Seed);
        run.Status.Should().Be("active");
    }

    [Fact]
    public async Task CreateDungeonRun_SessionStartedAgainUnderAnotherCommand_ReturnsItsFirstRun()
    {
        // Arrange
        Guid sessionId = Guid.NewGuid();
        CreateDungeonRunResponse first = await Client.CreateDungeonRunAsync(
            Request(Guid.NewGuid(), sessionId),
            cancellationToken: TestContext.Current.CancellationToken
        );

        // Act
        CreateDungeonRunResponse retried = await Client.CreateDungeonRunAsync(
            Request(Guid.NewGuid(), sessionId),
            cancellationToken: TestContext.Current.CancellationToken
        );

        // Assert
        retried.RunId.Should().Be(first.RunId);
        retried.Seed.Should().Be(first.Seed);
    }

    [Fact]
    public async Task CreateDungeonRun_CommandReusedForAnotherSession_IsAlreadyExists()
    {
        // Arrange
        Guid commandId = Guid.NewGuid();
        await Client.CreateDungeonRunAsync(
            Request(commandId, Guid.NewGuid()),
            cancellationToken: TestContext.Current.CancellationToken
        );

        // Act
        Func<Task> act = async () =>
            await Client.CreateDungeonRunAsync(
                Request(commandId, Guid.NewGuid()),
                cancellationToken: TestContext.Current.CancellationToken
            );

        // Assert
        (await act.Should().ThrowAsync<RpcException>())
            .Which.StatusCode.Should()
            .Be(StatusCode.AlreadyExists);
    }

    [Fact]
    public async Task CreateDungeonRun_InvalidSeed_IsAnInvalidArgument()
    {
        // Arrange
        CreateDungeonRunRequest request = Request(Guid.NewGuid(), Guid.NewGuid());
        request.Seed = "not a seed";

        // Act
        Func<Task> act = async () =>
            await Client.CreateDungeonRunAsync(
                request,
                cancellationToken: TestContext.Current.CancellationToken
            );

        // Assert
        (await act.Should().ThrowAsync<RpcException>())
            .Which.StatusCode.Should()
            .Be(StatusCode.InvalidArgument);
    }

    private DungeonRunService.DungeonRunServiceClient Client =>
        client ?? throw new InvalidOperationException("Fixture not initialized.");

    private static CreateDungeonRunRequest Request(Guid commandId, Guid sessionId) =>
        new()
        {
            CommandId = commandId.ToString(),
            GameSessionId = sessionId.ToString(),
            Participants = { new DungeonRunParticipant { HeroId = Guid.NewGuid().ToString() } },
        };

    public async ValueTask DisposeAsync()
    {
        channel?.Dispose();
        factory?.Dispose();
        if (database is not null)
            await database.DisposeAsync();
    }
}
