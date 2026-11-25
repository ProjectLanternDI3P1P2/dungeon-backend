using Dungeon.Application.Features.DungeonRunUseCase.OpenChest;
using Dungeon.Application.Messaging;
using Dungeon.Application.Models;
using Dungeon.Application.Ports;
using Dungeon.Domain.Entities;
using Dungeon.Domain.Enums;
using Dungeon.Domain.Exceptions;
using Dungeon.Domain.Mobs;
using Dungeon.Domain.Repositories;
using Dungeon.Domain.Services;
using Dungeon.Domain.ValueObjects;
using Dungeon.Test.TestSupport;
using FluentAssertions;
using Moq;

namespace Dungeon.Test.Features.DungeonRunUseCase.OpenChest;

public sealed class OpenChestCommandHandlerTests
{
    private readonly Mock<IDungeonRunRepository> _dungeonRunRepositoryMock = new();
    private readonly Mock<IMessagePublisher> _messagePublisherMock = new();
    private readonly Mock<IRewardClient> _rewardClientMock = new();
    private readonly Mock<IClock> _clockMock = new();
    private readonly GeneratedDungeon _dungeon = DungeonTestData.Generate(
        DungeonTestData.ReferenceSeed
    );
    private readonly DungeonRun _run;
    private readonly DungeonElement _chest;
    private readonly OpenChestCommandHandler _handler;

    public OpenChestCommandHandlerTests()
    {
        _run = DungeonRun.Start(Guid.NewGuid(), Guid.NewGuid(), _dungeon, DateTimeOffset.UtcNow);
        _chest = _dungeon.Floors[0].Elements.Single(element => element.Type == ElementType.Item);
        _dungeonRunRepositoryMock
            .Setup(repository => repository.GetByIdAsync(_run.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_run);
        _clockMock
            .SetupGet(clock => clock.UtcNow)
            .Returns(DateTimeOffset.Parse("2026-10-19T12:00:00Z"));
        _messagePublisherMock
            .Setup(publisher =>
                publisher.PublishAsync(It.IsAny<MessageEnvelope>(), It.IsAny<CancellationToken>())
            )
            .Returns(Task.CompletedTask);
        _rewardClientMock
            .Setup(client =>
                client.GenerateChestLootAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<Guid>(),
                    It.IsAny<Guid>(),
                    It.IsAny<int>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(
                new ChestLoot(
                    Guid.NewGuid(),
                    _run.Id,
                    Guid.NewGuid(),
                    1,
                    "NORMAL",
                    false,
                    [new ChestLootItem(Guid.NewGuid(), "Key", "COMMON", 1)]
                )
            );

        _handler = new OpenChestCommandHandler(
            _dungeonRunRepositoryMock.Object,
            new GeneratingDungeonProvider(),
            _clockMock.Object,
            _messagePublisherMock.Object,
            _rewardClientMock.Object
        );
    }

    [Fact]
    public async Task Handle_HeroStandingOnChest_OpensItAndEmitsOneRewardCause()
    {
        WalkTo(_chest.Position);
        int turn = _run.Turn;

        ChestLoot result = await _handler.Handle(
            new OpenChestCommand(_run.Id, _chest.Id),
            TestContext.Current.CancellationToken
        );

        _run.OpenedChests.Should()
            .ContainSingle(opened => opened.Floor == 0 && opened.ElementId == _chest.Id);
        _run.Turn.Should().Be(turn + 1);
        result.Items.Should().ContainSingle();
        _rewardClientMock.Verify(
            client =>
                client.GenerateChestLootAsync(
                    _chest.Id,
                    _run.Id,
                    _chest.Id,
                    1,
                    "NORMAL",
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );
        _messagePublisherMock.Verify(
            publisher =>
                publisher.PublishAsync(
                    It.Is<MessageEnvelope>(message => message.Type == "dungeon.chest.opened"),
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );
    }

    [Fact]
    public async Task Handle_HeroNotOnChest_RefusesAndDoesNotEmit()
    {
        Func<Task> act = () =>
            _handler.Handle(
                new OpenChestCommand(_run.Id, _chest.Id),
                TestContext.Current.CancellationToken
            );

        await act.Should().ThrowAsync<ChestNotInReachException>();
        _run.OpenedChests.Should().BeEmpty();
        _rewardClientMock.Verify(
            client =>
                client.GenerateChestLootAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<Guid>(),
                    It.IsAny<Guid>(),
                    It.IsAny<int>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()
                ),
            Times.Never
        );
        _messagePublisherMock.Verify(
            publisher =>
                publisher.PublishAsync(It.IsAny<MessageEnvelope>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
    }

    [Fact]
    public async Task Handle_HeroNextToChest_OpensIt()
    {
        Position adjacent = _chest.Position.Step(Direction.West);
        if (!_dungeon.Floors[0].IsWalkable(adjacent))
        {
            adjacent = _chest.Position.Step(Direction.East);
        }

        WalkTo(adjacent);

        await _handler.Handle(
            new OpenChestCommand(_run.Id, _chest.Id),
            TestContext.Current.CancellationToken
        );

        _run.OpenedChests.Should().ContainSingle();
    }

    [Fact]
    public async Task Handle_HeroDiagonallyNextToChest_OpensIt()
    {
        Position diagonal = new(_chest.Position.X - 1, _chest.Position.Y - 1);
        Position[] candidates =
        [
            diagonal,
            diagonal with
            {
                X = _chest.Position.X + 1,
            },
            diagonal with
            {
                Y = _chest.Position.Y + 1,
            },
            new(_chest.Position.X + 1, _chest.Position.Y + 1),
        ];
        Position reachable = candidates.First(position =>
            _dungeon.Floors[0].IsWalkable(position)
            && DungeonTestData.FindPath(_dungeon.Floors[0], _run.HeroPosition, position) is not null
        );
        WalkTo(reachable);

        await _handler.Handle(
            new OpenChestCommand(_run.Id, _chest.Id),
            TestContext.Current.CancellationToken
        );

        _run.OpenedChests.Should().ContainSingle();
    }

    [Fact]
    public async Task Handle_RetryingAnOpenedChest_IsIdempotent()
    {
        WalkTo(_chest.Position);
        OpenChestCommand command = new(_run.Id, _chest.Id);

        await _handler.Handle(command, TestContext.Current.CancellationToken);
        int turn = _run.Turn;
        await _handler.Handle(command, TestContext.Current.CancellationToken);

        _run.OpenedChests.Should().ContainSingle();
        _run.Turn.Should().Be(turn);
        _messagePublisherMock.Verify(
            publisher =>
                publisher.PublishAsync(It.IsAny<MessageEnvelope>(), It.IsAny<CancellationToken>()),
            Times.Once
        );
    }

    private void WalkTo(Position target)
    {
        DungeonFloor floor = _dungeon.Floors[_run.CurrentFloor];
        foreach (Direction direction in DungeonTestData.FindPath(floor, _run.HeroPosition, target)!)
        {
            _run.MoveHero(direction, _dungeon, MobRules.Default);
        }
    }
}
