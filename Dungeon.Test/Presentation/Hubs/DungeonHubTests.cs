using Dungeon.Application.Features.DungeonRunUseCase.GetDungeonRunById;
using Dungeon.Application.Features.DungeonRunUseCase.MoveHero;
using Dungeon.Domain.Exceptions;
using Dungeon.Domain.ValueObjects;
using Dungeon.Presentation.Hubs;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Caching.Memory;
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
    private readonly GetDungeonRunByIdResult _snapshot = new() { Id = DungeonId, Turn = 1 };
    private readonly DungeonHub _hub;

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

        _hub = new DungeonHub(
            _mediatorMock.Object,
            new HubCommandCache(new MemoryCache(new MemoryCacheOptions())),
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

    private static MoveHeroRequest Move(Guid commandId) => new(commandId, DungeonId, "east");
}
