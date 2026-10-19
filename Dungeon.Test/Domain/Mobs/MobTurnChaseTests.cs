using Dungeon.Domain.Entities;
using Dungeon.Domain.Enums;
using Dungeon.Domain.Mobs;
using Dungeon.Domain.ValueObjects;
using Dungeon.Test.TestSupport;
using FluentAssertions;

namespace Dungeon.Test.Domain.Mobs;

/// <summary>Aggro and chase (DEPLACEMENT_MOBS.fr.md, sections 3 to 5).</summary>
public class MobTurnChaseTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 18, 12, 0, 0, TimeSpan.Zero);

    /// <summary>A one-tile-high corridor that is a single room, walls around.</summary>
    private static (DungeonRun Run, GeneratedDungeon Dungeon) Corridor(string inside)
    {
        string wall = new('#', inside.Length + 2);
        GeneratedDungeon dungeon = DungeonTestData.Arena(
            [wall, $"#{inside}#", wall],
            new RoomBounds(1, 1, inside.Length, 1)
        );
        return (DungeonRun.Start(Guid.NewGuid(), Guid.NewGuid(), dungeon, Now), dungeon);
    }

    private static List<int> XsWhileMoving(
        DungeonRun run,
        GeneratedDungeon dungeon,
        Direction direction,
        int turns
    )
    {
        List<int> xs = [];
        for (int turn = 0; turn < turns; turn++)
        {
            run.MoveHero(direction, dungeon, MobRules.Default);
            xs.Add(run.Mobs[0].X);
        }

        return xs;
    }

    [Fact]
    public void Zombie_TenStepsAway_TurnsAggroAndStepsTowardTheHero()
    {
        // Arrange: the zombie is 11 steps away, 10 once the hero moves.
        (DungeonRun run, GeneratedDungeon dungeon) = Corridor("H..........z");

        // Act
        run.MoveHero(Direction.East, dungeon, MobRules.Default);

        // Assert
        run.Mobs[0].IsAggro.Should().BeTrue();
        run.Mobs[0].Position.Should().Be(new Position(11, 1));
    }

    [Fact]
    public void Zombie_ElevenStepsAway_StaysOutOfAggro()
    {
        // Arrange
        (DungeonRun run, GeneratedDungeon dungeon) = Corridor("H...........z");

        // Act
        run.MoveHero(Direction.East, dungeon, MobRules.Default);

        // Assert
        run.Mobs[0].IsAggro.Should().BeFalse();
    }

    [Fact]
    public void Zombie_ActsTwoTurnsOutOfThree_AndKeepsItsAggroOutOfRange()
    {
        // Arrange: aggro on the first move, then the hero runs away faster than it.
        (DungeonRun run, GeneratedDungeon dungeon) = Corridor("...........H........z");

        // Act
        List<int> xs = XsWhileMoving(run, dungeon, Direction.West, 6);

        // Assert: cadence 110 from its aggro, 11 steps behind at the end and still hunting.
        xs.Should().Equal(20, 19, 19, 18, 17, 17);
        run.Mobs[0].IsAggro.Should().BeTrue();
    }

    [Fact]
    public void Bat_StepsDiagonally()
    {
        // Arrange
        GeneratedDungeon dungeon = DungeonTestData.Arena(
            ["#######", "#H....#", "#.....#", "#.....#", "#..b..#", "#######"],
            new RoomBounds(1, 1, 5, 4)
        );
        DungeonRun run = DungeonRun.Start(Guid.NewGuid(), Guid.NewGuid(), dungeon, Now);

        // Act: 4 steps away once the hero moves south, two across and two down.
        run.MoveHero(Direction.South, dungeon, MobRules.Default);

        // Assert: one diagonal step closes both gaps at once.
        run.Mobs[0].Position.Should().Be(new Position(2, 3));
    }

    [Fact]
    public void Bat_RestsEveryFourthTurn()
    {
        // Arrange
        (DungeonRun run, GeneratedDungeon dungeon) = Corridor("..........H...b.");

        // Act
        List<int> xs = XsWhileMoving(run, dungeon, Direction.West, 5);

        // Assert: cadence 1110.
        xs.Should().Equal(14, 13, 12, 12, 11);
    }

    [Fact]
    public void Goblin_ThreeStepsAway_RushesEveryTurn()
    {
        // Arrange
        (DungeonRun run, GeneratedDungeon dungeon) = Corridor("..........H...g.");

        // Act
        run.MoveHero(Direction.East, dungeon, MobRules.Default);
        int first = run.Mobs[0].X;
        List<int> next = XsWhileMoving(run, dungeon, Direction.West, 2);

        // Assert
        run.Mobs[0].IsAggro.Should().BeTrue();
        new[] { first }.Concat(next).Should().Equal(14, 13, 12);
    }

    [Fact]
    public void Mob_FollowsTheHeroOutOfItsRoom()
    {
        // Arrange: the zombie's room is east of the door.
        GeneratedDungeon dungeon = DungeonTestData.Arena(
            ["#########", "#H..+.z.#", "#########"],
            new RoomBounds(1, 1, 3, 1),
            new RoomBounds(5, 1, 3, 1)
        );
        DungeonRun run = DungeonRun.Start(Guid.NewGuid(), Guid.NewGuid(), dungeon, Now);

        // Act
        Direction[] moves = [Direction.East, Direction.West, Direction.East, Direction.West];
        foreach (Direction move in moves)
        {
            run.MoveHero(move, dungeon, MobRules.Default);
        }

        // Assert: through the door into the hero's room.
        run.Mobs[0].Position.Should().Be(new Position(3, 1));
    }

    [Fact]
    public void Mob_BlockedByAnotherMob_Waits()
    {
        // Arrange: a mage (which does not chase yet on its first turn) stands in the way.
        (DungeonRun run, GeneratedDungeon dungeon) = Corridor(".H.mz");

        // Act
        run.MoveHero(Direction.West, dungeon, MobRules.Default);

        // Assert
        RunMob zombie = run.Mobs.Single(mob => mob.Kind == EnemyKind.Zombie);
        zombie.IsAggro.Should().BeTrue();
        zombie.Position.Should().Be(new Position(5, 1));
    }

    [Fact]
    public void Mob_CanStandOnATrap()
    {
        // Arrange
        (DungeonRun run, GeneratedDungeon dungeon) = Corridor("H.tz");

        // Act
        run.MoveHero(Direction.East, dungeon, MobRules.Default);

        // Assert
        run.Mobs[0].Position.Should().Be(new Position(3, 1));
    }
}
