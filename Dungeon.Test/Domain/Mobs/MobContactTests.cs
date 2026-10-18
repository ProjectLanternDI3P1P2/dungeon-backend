using Dungeon.Domain.Entities;
using Dungeon.Domain.Enums;
using Dungeon.Domain.Exceptions;
using Dungeon.Domain.ValueObjects;
using Dungeon.Test.TestSupport;
using FluentAssertions;

namespace Dungeon.Test.Domain.Mobs;

/// <summary>Touching a mob kills it, until Combat exists (DEPLACEMENT_MOBS.fr.md, section 6).</summary>
public class MobContactTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 18, 12, 0, 0, TimeSpan.Zero);

    private static (DungeonRun Run, GeneratedDungeon Dungeon) Room(params string[] inside)
    {
        string wall = new('#', inside[0].Length + 2);
        GeneratedDungeon dungeon = DungeonTestData.Arena(
            [wall, .. inside.Select(row => $"#{row}#"), wall],
            new RoomBounds(1, 1, inside[0].Length, inside.Length)
        );
        return (DungeonRun.Start(Guid.NewGuid(), Guid.NewGuid(), dungeon, Now), dungeon);
    }

    [Fact]
    public void Contact_OneTileAwayDiagonally_KillsTheMob()
    {
        // Arrange
        (DungeonRun run, GeneratedDungeon dungeon) = Room("H....", ".....", "..z..");

        // Act: the zombie steps next to the hero, diagonally.
        run.MoveHero(Direction.East, dungeon);

        // Assert
        run.Mobs[0].Position.Should().Be(new Position(3, 2));
        run.Mobs[0].IsActive.Should().BeFalse();
    }

    [Fact]
    public void KilledMob_NeverActsAgain()
    {
        // Arrange
        (DungeonRun run, GeneratedDungeon dungeon) = Room("H....", ".....", "..z..");
        run.MoveHero(Direction.East, dungeon);
        Position where = run.Mobs[0].Position;

        // Act
        run.MoveHero(Direction.South, dungeon);
        run.MoveHero(Direction.South, dungeon);

        // Assert
        run.Mobs[0].IsActive.Should().BeFalse();
        run.Mobs[0].Position.Should().Be(where);
    }

    [Fact]
    public void MoveHero_OntoAnActiveMob_IsRejectedAndChangesNothing()
    {
        // Arrange: a zombie stands next to the hero before the first turn.
        (DungeonRun run, GeneratedDungeon dungeon) = Room("Hz...");

        // Act
        Action act = () => run.MoveHero(Direction.East, dungeon);

        // Assert
        act.Should().Throw<InvalidMoveException>();
        run.HeroPosition.Should().Be(new Position(1, 1));
        run.Turn.Should().Be(0);
        run.Mobs[0].IsActive.Should().BeTrue();
    }
}
