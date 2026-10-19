using Dungeon.Application.Features.DungeonRunUseCase.MoveHero;
using Dungeon.Domain.Entities;
using Dungeon.Domain.Enums;
using Dungeon.Domain.Exceptions;
using Dungeon.Domain.Mobs;
using Dungeon.Domain.Repositories;
using Dungeon.Domain.ValueObjects;
using Dungeon.Test.TestSupport;
using FluentAssertions;
using Moq;

namespace Dungeon.Test.Features.DungeonRunUseCase.MoveHero;

public class MoveHeroCommandHandlerTests
{
    private readonly Mock<IDungeonRunRepository> _dungeonRunRepositoryMock = new();
    private readonly MoveHeroCommandHandler _handler;
    private readonly GeneratedDungeon _dungeon = DungeonTestData.Generate(
        DungeonTestData.ReferenceSeed
    );
    private readonly DungeonRun _run;

    public MoveHeroCommandHandlerTests()
    {
        _run = DungeonRun.Start(Guid.NewGuid(), Guid.NewGuid(), _dungeon, DateTimeOffset.UtcNow);
        _dungeonRunRepositoryMock
            .Setup(repository => repository.GetByIdAsync(_run.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_run);

        _handler = new MoveHeroCommandHandler(
            _dungeonRunRepositoryMock.Object,
            new GeneratingDungeonProvider(),
            MobRules.Default
        );
    }

    [Fact]
    public async Task Handle_PlaysTheMobsWithTheConfiguredRules()
    {
        // Arrange: rules under which every mob of the floor is in range and always acts.
        MobBehaviour hunter = new("1", 1000, MobStep.Orthogonal, MobIdle.None);
        MobRules everyoneHunts = new(
            2,
            Enum.GetValues<EnemyKind>().ToDictionary(kind => kind, _ => hunter)
        );
        MoveHeroCommandHandler handler = new(
            _dungeonRunRepositoryMock.Object,
            new GeneratingDungeonProvider(),
            everyoneHunts
        );
        Position centre = WalkToTheCentreOfTheStartRoom();

        // Act
        await handler.Handle(
            new MoveHeroCommand(_run.Id, "east"),
            TestContext.Current.CancellationToken
        );

        // Assert
        _run.Mobs.Should().NotBeEmpty();
        _run.Mobs.Should().OnlyContain(mob => mob.IsAggro);
        _run.HeroPosition.Should().Be(new Position(centre.X + 1, centre.Y));
    }

    [Theory]
    [InlineData("east", 1, 0)]
    [InlineData("WEST", -1, 0)]
    [InlineData("North", 0, -1)]
    [InlineData("south", 0, 1)]
    public async Task Handle_TowardsAFloorTile_MovesTheHeroOneTile(
        string direction,
        int deltaX,
        int deltaY
    )
    {
        // Arrange: from the centre of the start room, whose neighbours are all floor.
        Position centre = WalkToTheCentreOfTheStartRoom();
        int turn = _run.Turn;

        // Act
        await _handler.Handle(
            new MoveHeroCommand(_run.Id, direction),
            TestContext.Current.CancellationToken
        );

        // Assert
        _run.HeroPosition.Should().Be(new Position(centre.X + deltaX, centre.Y + deltaY));
        _run.Turn.Should().Be(turn + 1);
    }

    [Fact]
    public async Task Handle_TowardsAWall_ThrowsAndLeavesTheHeroInPlace()
    {
        // Arrange: walk west from the centre of the start room until something blocks the way.
        DungeonFloor floor = _dungeon.Floors[0];
        WalkToTheCentreOfTheStartRoom();
        _run.MoveHero(Direction.North, _dungeon, MobRules.Default);
        while (floor.IsWalkable(_run.HeroPosition.Step(Direction.West)))
        {
            _run.MoveHero(Direction.West, _dungeon, MobRules.Default);
        }

        Position before = _run.HeroPosition;

        // Act
        Func<Task> act = async () =>
            await _handler.Handle(
                new MoveHeroCommand(_run.Id, "west"),
                TestContext.Current.CancellationToken
            );

        // Assert
        await act.Should().ThrowAsync<InvalidMoveException>();
        _run.HeroPosition.Should().Be(before);
    }

    [Fact]
    public async Task Handle_IntoTheOpenGate_GoesDownToTheNextFloor()
    {
        // Arrange: the boss of the first floor is defeated, the hero stands below the gate.
        DungeonFloor floor = _dungeon.Floors[0];
        Position boss = floor.Elements.Single(element => element.Type == ElementType.Boss).Position;
        WalkTo(boss);
        _run.DefeatFloorBoss(_dungeon);
        WalkTo(DungeonTestData.PositionsOf(floor, CellType.Gate).Single().Step(Direction.South));

        // Act
        await _handler.Handle(
            new MoveHeroCommand(_run.Id, "north"),
            TestContext.Current.CancellationToken
        );

        // Assert
        _run.CurrentFloor.Should().Be(1);
        _run.HeroPosition.Should().Be(_dungeon.Floors[1].Entrance);
    }

    [Fact]
    public async Task Handle_UnknownRun_ThrowsKeyNotFoundException()
    {
        // Arrange
        var runId = Guid.NewGuid();

        // Act
        Func<Task> act = async () =>
            await _handler.Handle(
                new MoveHeroCommand(runId, "east"),
                TestContext.Current.CancellationToken
            );

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>().WithMessage($"*{runId}*");
    }

    private Position WalkToTheCentreOfTheStartRoom()
    {
        Position centre = _dungeon.Floors[0].Rooms[0].Center;
        WalkTo(centre);
        return centre;
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
