using Dungeon.Domain.Entities;
using Dungeon.Domain.Enums;
using Dungeon.Domain.Mobs;
using Dungeon.Domain.ValueObjects;
using Dungeon.Test.TestSupport;
using FluentAssertions;

namespace Dungeon.Test.Domain.Entities;

/// <summary>The mobs of the current floor, kept in the run (DEPLACEMENT_MOBS.fr.md, section 2).</summary>
public class DungeonRunMobsTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 18, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Start_SpawnsOneMobPerEnemyOfTheFirstFloor()
    {
        // Arrange
        GeneratedDungeon dungeon = DungeonTestData.Generate(DungeonTestData.ReferenceSeed);
        DungeonFloor floor = dungeon.Floors[0];

        // Act
        DungeonRun run = DungeonRun.Start(Guid.NewGuid(), Guid.NewGuid(), dungeon, Now);

        // Assert
        run.Mobs.Select(mob => (mob.ElementId, mob.Kind, mob.Position, mob.HomeRoomId))
            .Should()
            .Equal(
                floor
                    .Elements.Where(element => element.Type == ElementType.Enemy)
                    .Select(element =>
                        (element.Id, element.Kind!.Value, element.Position, element.RoomId)
                    )
            );
        run.Mobs.Should().OnlyContain(mob => mob.IsActive && !mob.IsAggro);
    }

    [Fact]
    public void Start_DiscoversTheRoomOfTheEntrance()
    {
        // Arrange
        GeneratedDungeon dungeon = DungeonTestData.Generate(DungeonTestData.ReferenceSeed);

        // Act
        DungeonRun run = DungeonRun.Start(Guid.NewGuid(), Guid.NewGuid(), dungeon, Now);

        // Assert
        run.DiscoveredRoomIds.Should().Equal(0);
    }

    [Fact]
    public void GoDown_ReplacesTheMobsWithThoseOfTheNextFloor()
    {
        // Arrange
        GeneratedDungeon dungeon = DungeonTestData.Generate(DungeonTestData.ReferenceSeed);
        DungeonFloor floor = dungeon.Floors[0];
        DungeonRun run = DungeonRun.Start(Guid.NewGuid(), Guid.NewGuid(), dungeon, Now);
        Position boss = floor.Elements.Single(element => element.Type == ElementType.Boss).Position;
        DungeonTestData.WalkTo(run, dungeon, boss);
        run.DefeatFloorBoss(dungeon);
        Position gate = DungeonTestData.PositionsOf(floor, CellType.Gate).Single();
        DungeonTestData.WalkTo(run, dungeon, gate.Step(Direction.South));

        // Act
        run.MoveHero(Direction.North, dungeon, MobRules.Default);

        // Assert
        run.CurrentFloor.Should().Be(1);
        run.Mobs.Select(mob => mob.ElementId)
            .Should()
            .Equal(
                dungeon
                    .Floors[1]
                    .Elements.Where(element => element.Type == ElementType.Enemy)
                    .Select(element => element.Id)
            );
        run.Mobs.Should().OnlyContain(mob => mob.IsActive && !mob.IsAggro);
        run.DiscoveredRoomIds.Should().Equal(0);
    }
}
