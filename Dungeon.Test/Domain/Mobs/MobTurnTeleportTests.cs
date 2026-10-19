using Dungeon.Domain.Entities;
using Dungeon.Domain.Enums;
using Dungeon.Domain.ValueObjects;
using Dungeon.Test.TestSupport;
using FluentAssertions;

namespace Dungeon.Test.Domain.Mobs;

/// <summary>The mage jumps to a tile it announced the turn before (section 5).</summary>
public class MobTurnTeleportTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 18, 12, 0, 0, TimeSpan.Zero);

    private static (DungeonRun Run, GeneratedDungeon Dungeon) Corridor(string inside)
    {
        string wall = new('#', inside.Length + 2);
        GeneratedDungeon dungeon = DungeonTestData.Arena(
            [wall, $"#{inside}#", wall],
            new RoomBounds(1, 1, inside.Length, 1)
        );
        return (DungeonRun.Start(Guid.NewGuid(), Guid.NewGuid(), dungeon, Now), dungeon);
    }

    private static Direction Pace(int turn) => turn % 2 == 0 ? Direction.East : Direction.West;

    private static RunMob Mage(DungeonRun run) =>
        run.Mobs.Single(mob => mob.Kind == EnemyKind.Mage);

    [Fact]
    public void Mage_IsAggro_AsSoonAsItsRoomIsDiscovered()
    {
        // Arrange: the mage's room lies east of the door (4, 1), far from the hero.
        GeneratedDungeon dungeon = DungeonTestData.Arena(
            ["####################", "#H..+.............m#", "####################"],
            new RoomBounds(1, 1, 3, 1),
            new RoomBounds(5, 1, 14, 1)
        );
        DungeonRun run = DungeonRun.Start(Guid.NewGuid(), Guid.NewGuid(), dungeon, Now);

        // Act
        run.MoveHero(Direction.East, dungeon);
        run.MoveHero(Direction.East, dungeon);
        bool aggroBeforeTheDoor = Mage(run).IsAggro;
        run.MoveHero(Direction.East, dungeon);

        // Assert
        aggroBeforeTheDoor.Should().BeFalse();
        Mage(run).IsAggro.Should().BeTrue();
    }

    [Fact]
    public void Mage_InAnUndiscoveredRoom_StaysStill()
    {
        // Arrange
        GeneratedDungeon dungeon = DungeonTestData.Arena(
            ["####################", "#H..+.............m#", "####################"],
            new RoomBounds(1, 1, 3, 1),
            new RoomBounds(5, 1, 14, 1)
        );
        DungeonRun run = DungeonRun.Start(Guid.NewGuid(), Guid.NewGuid(), dungeon, Now);

        // Act
        for (int turn = 0; turn < 6; turn++)
        {
            run.MoveHero(Pace(turn), dungeon);
        }

        // Assert
        Mage(run).IsAggro.Should().BeFalse();
        Mage(run).Position.Should().Be(new Position(18, 1));
    }

    [Fact]
    public void Mage_AnnouncesItsJumpOneTurnBefore_ThenLandsThere()
    {
        // Arrange: in the start room, so in aggro from the first turn; cadence 001.
        (DungeonRun run, GeneratedDungeon dungeon) = Corridor("H.........m");

        // Act
        run.MoveHero(Pace(0), dungeon);
        Position? afterFirst = Mage(run).Teleport;
        run.MoveHero(Pace(1), dungeon);
        Position? announced = Mage(run).Teleport;
        Position before = Mage(run).Position;
        run.MoveHero(Pace(2), dungeon);

        // Assert: three steps toward the hero, announced on the turn before the jump.
        afterFirst.Should().BeNull();
        announced.Should().Be(new Position(8, 1));
        before.Should().Be(new Position(11, 1));
        Mage(run).Position.Should().Be(new Position(8, 1));
        Mage(run).Teleport.Should().BeNull();
    }

    [Fact]
    public void Mage_JumpsDiagonally_ToTheTileClosestToTheHero()
    {
        // Arrange: an open room, the hero in a corner and the mage four tiles away on both axes.
        GeneratedDungeon dungeon = DungeonTestData.Arena(
            ["#######", "#H....#", "#.....#", "#.....#", "#.....#", "#....m#", "#######"],
            new RoomBounds(1, 1, 5, 5)
        );
        DungeonRun run = DungeonRun.Start(Guid.NewGuid(), Guid.NewGuid(), dungeon, Now);

        // Act: the hero paces, the mage announces on the second turn.
        run.MoveHero(Direction.East, dungeon);
        run.MoveHero(Direction.West, dungeon);

        // Assert: three diagonal steps, two tiles from the hero.
        Mage(run).Teleport.Should().Be(new Position(2, 2));
    }

    [Fact]
    public void Mage_AnnouncedTileTaken_StaysAndAnnouncesAgain()
    {
        // Arrange: the hero walks onto the tile the mage announced.
        (DungeonRun run, GeneratedDungeon dungeon) = Corridor("H.....m");

        // Act
        run.MoveHero(Direction.East, dungeon);
        run.MoveHero(Direction.East, dungeon);
        Position? announced = Mage(run).Teleport;
        run.MoveHero(Direction.East, dungeon);

        // Assert
        announced.Should().Be(new Position(4, 1));
        run.HeroPosition.Should().Be(new Position(4, 1));
        Mage(run).Position.Should().Be(new Position(7, 1));
        Mage(run).Teleport.Should().Be(new Position(5, 1));
    }

    [Fact]
    public void Mage_NeverLandsOnTheHero()
    {
        // Arrange: three steps away, the jump would end on the hero.
        (DungeonRun run, GeneratedDungeon dungeon) = Corridor("H..m....");

        for (int turn = 0; turn < 9; turn++)
        {
            // Act
            run.MoveHero(Pace(turn), dungeon);

            // Assert: the hero may walk onto the announced tile, the mage never lands there.
            Mage(run).Position.Should().NotBe(run.HeroPosition);
        }
    }
}
