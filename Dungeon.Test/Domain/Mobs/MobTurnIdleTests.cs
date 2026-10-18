using Dungeon.Domain.Entities;
using Dungeon.Domain.Enums;
using Dungeon.Domain.ValueObjects;
using Dungeon.Test.TestSupport;
using FluentAssertions;

namespace Dungeon.Test.Domain.Mobs;

/// <summary>What mobs do before their aggro (DEPLACEMENT_MOBS.fr.md, section 5).</summary>
public class MobTurnIdleTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 18, 12, 0, 0, TimeSpan.Zero);

    /// <summary>
    /// The start room (x 1-3) opens through the door (4, 1) onto a long room (x 5-19, centre
    /// (12, 2)), which has a second door east, (20, 2). The mob tile is replaced by its letter.
    /// </summary>
    private static (DungeonRun Run, GeneratedDungeon Dungeon) TwoRooms(char mob, Position at)
    {
        char[][] rows =
        [
            [.. "######################"],
            [.. "#H..+...............##"],
            [.. "#...#...............+#"],
            [.. "#...#...............##"],
            [.. "######################"],
        ];
        rows[at.Y][at.X] = mob;
        GeneratedDungeon dungeon = DungeonTestData.Arena(
            [.. rows.Select(row => new string(row))],
            new RoomBounds(1, 1, 3, 3),
            new RoomBounds(5, 1, 15, 3)
        );
        return (DungeonRun.Start(Guid.NewGuid(), Guid.NewGuid(), dungeon, Now), dungeon);
    }

    private static void PaceNorthAndSouth(DungeonRun run, GeneratedDungeon dungeon, int turns)
    {
        for (int turn = 0; turn < turns; turn++)
        {
            run.MoveHero(turn % 2 == 0 ? Direction.South : Direction.North, dungeon);
        }
    }

    private static int StepsTo(Position from, Position to) =>
        Math.Abs(from.X - to.X) + Math.Abs(from.Y - to.Y);

    [Fact]
    public void Wander_FarFromTheCentre_WalksTowardIt()
    {
        // Arrange: 20 steps from the hero, out of its aggro range.
        (DungeonRun run, GeneratedDungeon dungeon) = TwoRooms('z', new Position(19, 3));
        Position centre = new(12, 2);

        // Act
        PaceNorthAndSouth(run, dungeon, 1);

        // Assert
        run.Mobs[0].IsAggro.Should().BeFalse();
        StepsTo(run.Mobs[0].Position, centre).Should().Be(7);
    }

    [Fact]
    public void Wander_NearTheCentre_StrollsWithoutLeavingItsRoom()
    {
        // Arrange: a bat, whose short aggro range never reaches the start room from there.
        (DungeonRun run, GeneratedDungeon dungeon) = TwoRooms('b', new Position(13, 2));
        DungeonFloor floor = dungeon.Floors[0];
        HashSet<Position> visited = [];

        // Act
        for (int turn = 0; turn < 50; turn++)
        {
            run.MoveHero(turn % 2 == 0 ? Direction.South : Direction.North, dungeon);
            visited.Add(run.Mobs[0].Position);
        }

        // Assert
        run.Mobs[0].IsAggro.Should().BeFalse();
        visited.Should().OnlyContain(tile => floor.GetRoomId(tile) == 1);
        visited.Should().HaveCountGreaterThan(1, "a wandering mob does not stand still");
    }

    [Fact]
    public void Goblin_BeforeItsRoomIsDiscovered_StaysStill()
    {
        // Arrange
        (DungeonRun run, GeneratedDungeon dungeon) = TwoRooms('g', new Position(17, 2));

        // Act
        PaceNorthAndSouth(run, dungeon, 6);

        // Assert
        run.DiscoveredRoomIds.Should().NotContain(1);
        run.Mobs[0].Position.Should().Be(new Position(17, 2));
    }

    [Fact]
    public void Goblin_OnceItsRoomIsDiscovered_HoldsTheNearestDoor()
    {
        // Arrange: the east door is 3 steps away, the west one 13.
        (DungeonRun run, GeneratedDungeon dungeon) = TwoRooms('g', new Position(17, 2));

        // Act: the hero stands on the west door, discovering the room, then paces.
        run.MoveHero(Direction.East, dungeon);
        run.MoveHero(Direction.East, dungeon);
        run.MoveHero(Direction.East, dungeon);
        for (int turn = 0; turn < 6; turn++)
        {
            run.MoveHero(turn % 2 == 0 ? Direction.West : Direction.East, dungeon);
        }

        // Assert
        run.DiscoveredRoomIds.Should().Contain(1);
        run.Mobs[0].IsAggro.Should().BeFalse();
        run.Mobs[0].Position.Should().Be(new Position(20, 2));
    }

    [Fact]
    public void SameSeedSameMoves_GiveTheSameMobsEveryTurn()
    {
        // Arrange
        GeneratedDungeon dungeon = DungeonTestData.Generate(DungeonTestData.ReferenceSeed);
        DungeonFloor floor = dungeon.Floors[0];
        DungeonRun first = DungeonRun.Start(Guid.NewGuid(), Guid.NewGuid(), dungeon, Now);
        DungeonRun second = DungeonRun.Start(Guid.NewGuid(), Guid.NewGuid(), dungeon, Now);
        Position boss = floor.Elements.Single(element => element.Type == ElementType.Boss).Position;
        List<Direction> path = DungeonTestData.FindPath(floor, floor.Entrance, boss)!;

        foreach (Direction direction in path.Take(30))
        {
            // Act
            first.MoveHero(direction, dungeon);
            second.MoveHero(direction, dungeon);

            // Assert
            second
                .Mobs.Select(mob => (mob.Position, mob.IsAggro, mob.IsActive))
                .Should()
                .Equal(first.Mobs.Select(mob => (mob.Position, mob.IsAggro, mob.IsActive)));
        }
    }
}
