using Dungeon.Application.Features.DungeonRunUseCase.GetDungeonRunById;
using Dungeon.Application.Features.DungeonRunUseCase.MoveHero;
using Dungeon.Application.Features.DungeonRunUseCase.SpawnMob;
using Dungeon.Application.Models;
using Dungeon.Domain.Exceptions;
using Dungeon.Domain.ValueObjects;
using Dungeon.Presentation.Hubs;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Hosting;
using Moq;
using Serilog.Core;

namespace Dungeon.Test.Presentation.Hubs;

public class DungeonHubTests
{
    private const string ConnectionId = "connection-1";
    private static readonly Guid DungeonId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    private readonly Mock<IMediator> _mediatorMock = new();
    private readonly Mock<IDungeonClient> _groupMock = new();
    private readonly Mock<IGroupManager> _groupsMock = new();
    private readonly Mock<IHostEnvironment> _environmentMock = new();
    private readonly GetDungeonRunByIdResult _snapshot = new() { Id = DungeonId, Turn = 1 };
    private readonly DungeonHub _hub;

    private readonly Mock<Dungeon.Application.Services.IEncounterCombatStarter> _encounterStarterMock =
        new();

    public DungeonHubTests()
    {
        _mediatorMock
            .Setup(mediator =>
                mediator.Send(It.IsAny<GetDungeonRunByIdQuery>(), It.IsAny<CancellationToken>())
            )
            .ReturnsAsync(_snapshot);

        var clientsMock = new Mock<IHubCallerClients<IDungeonClient>>();
        clientsMock
            .Setup(clients => clients.Group(DungeonHub.GroupName(DungeonId)))
            .Returns(_groupMock.Object);

        var contextMock = new Mock<HubCallerContext>();
        contextMock.SetupGet(context => context.ConnectionId).Returns(ConnectionId);
        contextMock.SetupGet(context => context.ConnectionAborted).Returns(CancellationToken.None);

        RunIn(Environments.Production);

        _encounterStarterMock
            .Setup(starter =>
                starter.StartPendingAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())
            )
            .ReturnsAsync(true);
        _hub = new DungeonHub(
            _mediatorMock.Object,
            new HubCommandCache(new MemoryCache(new MemoryCacheOptions())),
            _encounterStarterMock.Object,
            _environmentMock.Object,
            Logger.None
        )
        {
            Clients = clientsMock.Object,
            Context = contextMock.Object,
            Groups = _groupsMock.Object,
        };
    }

    [Fact]
    public async Task JoinDungeon_KnownRun_AddsTheConnectionToTheRunGroupAndReturnsTheSnapshot()
    {
        // Act
        DungeonCommandResult result = await _hub.JoinDungeon(new JoinDungeonRequest(DungeonId));

        // Assert
        result.Accepted.Should().BeTrue();
        result.Snapshot.Should().BeSameAs(_snapshot);
        _groupsMock.Verify(groups =>
            groups.AddToGroupAsync(
                ConnectionId,
                "dungeon:" + DungeonId,
                It.IsAny<CancellationToken>()
            )
        );
    }

    [Fact]
    public async Task JoinDungeon_UnknownRun_IsRefusedWithoutJoiningAGroup()
    {
        // Arrange
        _mediatorMock
            .Setup(mediator =>
                mediator.Send(It.IsAny<GetDungeonRunByIdQuery>(), It.IsAny<CancellationToken>())
            )
            .ThrowsAsync(new KeyNotFoundException("Dungeon run not found."));

        // Act
        DungeonCommandResult result = await _hub.JoinDungeon(new JoinDungeonRequest(DungeonId));

        // Assert
        result.Accepted.Should().BeFalse();
        result.Error!.Code.Should().Be(DungeonCommandErrorCodes.NotFound);
        _groupsMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task MoveHero_Accepted_BroadcastsTheSnapshotToTheRunGroup()
    {
        // Act
        DungeonCommandResult result = await _hub.MoveHero(Move(Guid.NewGuid()));

        // Assert
        result.Accepted.Should().BeTrue();
        result.Snapshot.Should().BeSameAs(_snapshot);
        _mediatorMock.Verify(mediator =>
            mediator.Send(new MoveHeroCommand(DungeonId, "east"), It.IsAny<CancellationToken>())
        );
        _groupMock.Verify(group => group.DungeonStateChanged(_snapshot), Times.Once);
    }

    [Fact]
    public async Task MoveHero_SameCommandIdTwice_AppliesTheMoveOnce()
    {
        // Arrange
        Guid commandId = Guid.NewGuid();

        // Act
        await _hub.MoveHero(Move(commandId));
        DungeonCommandResult retried = await _hub.MoveHero(Move(commandId));

        // Assert
        retried.Accepted.Should().BeTrue();
        _mediatorMock.Verify(
            mediator => mediator.Send(It.IsAny<MoveHeroCommand>(), It.IsAny<CancellationToken>()),
            Times.Once
        );
    }

    [Fact]
    public async Task MoveHero_RefusedByAGameRule_ReturnsTheReasonAndBroadcastsNothing()
    {
        // Arrange
        _mediatorMock
            .Setup(mediator =>
                mediator.Send(It.IsAny<MoveHeroCommand>(), It.IsAny<CancellationToken>())
            )
            .ThrowsAsync(
                new InvalidMoveException(new Position(1, 0), "a Wall tile is not walkable")
            );

        // Act
        DungeonCommandResult result = await _hub.MoveHero(Move(Guid.NewGuid()));

        // Assert
        result.Accepted.Should().BeFalse();
        result.Error!.Code.Should().Be(DungeonCommandErrorCodes.Refused);
        _groupMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task MoveHero_RefusedThenRetriedWithTheSameCommandId_IsEvaluatedAgain()
    {
        // Arrange
        Guid commandId = Guid.NewGuid();
        _mediatorMock
            .SetupSequence(mediator =>
                mediator.Send(It.IsAny<MoveHeroCommand>(), It.IsAny<CancellationToken>())
            )
            .ThrowsAsync(
                new InvalidMoveException(new Position(1, 0), "a Wall tile is not walkable")
            )
            .Returns(Task.CompletedTask);

        // Act
        await _hub.MoveHero(Move(commandId));
        DungeonCommandResult retried = await _hub.MoveHero(Move(commandId));

        // Assert
        retried.Accepted.Should().BeTrue();
    }

    [Fact]
    public async Task MoveHero_WithoutCommandId_IsRefusedAsInvalid()
    {
        // Act
        DungeonCommandResult result = await _hub.MoveHero(Move(Guid.Empty));

        // Assert
        result.Accepted.Should().BeFalse();
        result.Error!.Code.Should().Be(DungeonCommandErrorCodes.Validation);
        _mediatorMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task MoveHero_WhileAFightIsInProgress_IsRefusedAsInCombat()
    {
        // Arrange
        _mediatorMock
            .Setup(mediator =>
                mediator.Send(It.IsAny<MoveHeroCommand>(), It.IsAny<CancellationToken>())
            )
            .ThrowsAsync(new EncounterInProgressException(DungeonId, Guid.NewGuid()));

        // Act
        DungeonCommandResult result = await _hub.MoveHero(Move(Guid.NewGuid()));

        // Assert
        result.Accepted.Should().BeFalse();
        result.Error!.Code.Should().Be(DungeonCommandErrorCodes.InCombat);
        _groupMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task MoveHero_NoFightWaiting_ReadsTheRunOnceAndNeverCallsCombat()
    {
        // Act
        await _hub.MoveHero(Move(Guid.NewGuid()));

        // Assert
        VerifySnapshotReads(Times.Once());
        VerifyCombatStarts(Times.Never());
    }

    [Theory]
    [InlineData("pending")]
    [InlineData("failed")]
    public async Task MoveHero_ContactLeftAFightWaiting_OpensItAndBroadcastsTheRefreshedSnapshot(
        string status
    )
    {
        // Arrange
        GetDungeonRunByIdResult refreshed = new() { Id = DungeonId, Turn = 2 };
        ReadSnapshots(Waiting(status), refreshed);

        // Act
        DungeonCommandResult result = await _hub.MoveHero(Move(Guid.NewGuid()));

        // Assert
        result.Accepted.Should().BeTrue();
        result.Snapshot.Should().BeSameAs(refreshed);
        VerifyCombatStarts(Times.Once());
        VerifySnapshotReads(Times.Exactly(2));
        _groupMock.Verify(group => group.DungeonStateChanged(refreshed), Times.Once);
    }

    [Theory]
    [InlineData("active")]
    [InlineData("resolved")]
    public async Task MoveHero_FightAlreadyOpenOrSettled_DoesNotCallCombat(string status)
    {
        // Arrange
        ReadSnapshots(Waiting(status));

        // Act
        await _hub.MoveHero(Move(Guid.NewGuid()));

        // Assert
        VerifyCombatStarts(Times.Never());
        VerifySnapshotReads(Times.Once());
    }

    [Fact]
    public async Task RetryEncounter_FailedEncounter_AsksCombatAgainAndReturnsTheRefreshedSnapshot()
    {
        // Arrange
        GetDungeonRunByIdResult refreshed = new() { Id = DungeonId, Turn = 2 };
        ReadSnapshots(Waiting("failed"), refreshed);

        // Act
        DungeonCommandResult result = await _hub.RetryEncounter(
            new RetryEncounterRequest(Guid.NewGuid(), DungeonId)
        );

        // Assert
        result.Accepted.Should().BeTrue();
        result.Snapshot.Should().BeSameAs(refreshed);
        VerifyCombatStarts(Times.Once());
        _mediatorMock.Verify(
            mediator => mediator.Send(It.IsAny<MoveHeroCommand>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
        _groupMock.Verify(group => group.DungeonStateChanged(refreshed), Times.Once);
    }

    [Fact]
    public async Task RetryEncounter_NothingWaiting_DoesNotCallCombat()
    {
        // Act
        DungeonCommandResult result = await _hub.RetryEncounter(
            new RetryEncounterRequest(Guid.NewGuid(), DungeonId)
        );

        // Assert
        result.Accepted.Should().BeTrue();
        result.Snapshot.Should().BeSameAs(_snapshot);
        VerifyCombatStarts(Times.Never());
        VerifySnapshotReads(Times.Once());
    }

    [Theory]
    [InlineData("pending")]
    [InlineData("failed")]
    public async Task JoinDungeon_FightWaiting_AsksCombatToOpenItThenJoins(string status)
    {
        // Arrange
        GetDungeonRunByIdResult refreshed = new() { Id = DungeonId, Turn = 2 };
        ReadSnapshots(Waiting(status), refreshed);

        // Act
        DungeonCommandResult result = await _hub.JoinDungeon(new JoinDungeonRequest(DungeonId));

        // Assert
        result.Accepted.Should().BeTrue();
        result.Snapshot.Should().BeSameAs(refreshed);
        VerifyCombatStarts(Times.Once());
        _groupsMock.Verify(groups =>
            groups.AddToGroupAsync(
                ConnectionId,
                DungeonHub.GroupName(DungeonId),
                It.IsAny<CancellationToken>()
            )
        );
    }

    [Fact]
    public async Task JoinDungeon_NoFightWaiting_DoesNotCallCombatAndReadsTheRunOnce()
    {
        // Act
        await _hub.JoinDungeon(new JoinDungeonRequest(DungeonId));

        // Assert
        VerifyCombatStarts(Times.Never());
        VerifySnapshotReads(Times.Once());
    }

    [Fact]
    public async Task SpawnMob_InDevelopment_SendsTheCommand()
    {
        // Arrange
        RunIn(Environments.Development);

        // Act
        DungeonCommandResult result = await _hub.SpawnMob(Spawn());

        // Assert
        result.Accepted.Should().BeTrue();
        _mediatorMock.Verify(mediator =>
            mediator.Send(
                new SpawnMobCommand(DungeonId, "zombie", 3, 4),
                It.IsAny<CancellationToken>()
            )
        );
    }

    [Fact]
    public async Task SpawnMob_OutsideDevelopment_IsRefusedWithoutSendingAnything()
    {
        // Act
        DungeonCommandResult result = await _hub.SpawnMob(Spawn());

        // Assert
        result.Accepted.Should().BeFalse();
        result.Error!.Code.Should().Be(DungeonCommandErrorCodes.Refused);
        _mediatorMock.VerifyNoOtherCalls();
    }

    private static GetDungeonRunByIdResult Waiting(string status) =>
        new()
        {
            Id = DungeonId,
            Turn = 1,
            Encounter = new EncounterResult(
                Guid.NewGuid(),
                status,
                "collision",
                null,
                [Guid.NewGuid()],
                null,
                null
            ),
        };

    /// <summary>The successive answers of the run query: the last one repeats.</summary>
    private void ReadSnapshots(params GetDungeonRunByIdResult[] snapshots)
    {
        var sequence = _mediatorMock.SetupSequence(mediator =>
            mediator.Send(It.IsAny<GetDungeonRunByIdQuery>(), It.IsAny<CancellationToken>())
        );
        foreach (GetDungeonRunByIdResult snapshot in snapshots)
        {
            sequence = sequence.ReturnsAsync(snapshot);
        }

        sequence.ReturnsAsync(snapshots[^1]);
    }

    private void VerifySnapshotReads(Times times) =>
        _mediatorMock.Verify(
            mediator =>
                mediator.Send(It.IsAny<GetDungeonRunByIdQuery>(), It.IsAny<CancellationToken>()),
            times
        );

    private void VerifyCombatStarts(Times times) =>
        _encounterStarterMock.Verify(
            starter => starter.StartPendingAsync(DungeonId, It.IsAny<CancellationToken>()),
            times
        );

    private void RunIn(string environmentName) =>
        _environmentMock
            .SetupGet(environment => environment.EnvironmentName)
            .Returns(environmentName);

    private static SpawnMobRequest Spawn() => new(Guid.NewGuid(), DungeonId, "zombie", 3, 4);

    private static MoveHeroRequest Move(Guid commandId) => new(commandId, DungeonId, "east");
}
