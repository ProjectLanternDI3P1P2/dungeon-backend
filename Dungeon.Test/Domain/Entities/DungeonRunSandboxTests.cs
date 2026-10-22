using Dungeon.Domain.Entities;
using Dungeon.Domain.Enums;
using Dungeon.Domain.Exceptions;
using Dungeon.Domain.Mobs;
using Dungeon.Domain.Services.Generation;
using Dungeon.Domain.ValueObjects;
using Dungeon.Test.TestSupport;
using FluentAssertions;

namespace Dungeon.Test.Domain.Entities;

/// <summary>The debug sandbox: two bare rooms where mobs are spawned by hand, in development only.</summary>
public class DungeonRunSandboxTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 21, 12, 0, 0, TimeSpan.Zero);

    private static GeneratedDungeon GenerateSandbox() =>
        new DungeonGenerator(sandboxEnabled: true).Generate(
            SandboxDungeon.Seed,
            DungeonSettings.Default
        );

    private static (DungeonRun Run, GeneratedDungeon Dungeon) StartSandbox()
    {
        GeneratedDungeon dungeon = GenerateSandbox();
        return (DungeonRun.Start(Guid.NewGuid(), Guid.NewGuid(), dungeon, Now), dungeon);
    }

    [Fact]
    public void Generate_SandboxSeed_GivesTwoBareRoomsJoinedByACorridorOnEveryFloor()
    {
        // Act
        GeneratedDungeon dungeon = GenerateSandbox();

        // Assert
        dungeon.Floors.Should().HaveCount(DungeonSettings.Default.FloorCount);
        foreach (DungeonFloor floor in dungeon.Floors)
        {
            floor.Rooms.Select(room => room.Id).Should().Equal(0, 1);
            floor.Rooms[1].Interior.Bottom.Should().BeLessThan(floor.Rooms[0].Interior.Y);
            floor.Elements.Should().BeEmpty();
            DungeonTestData.PositionsOf(floor, CellType.Door).Should().HaveCount(2);
            floor.GetRoomId(floor.Entrance).Should().Be(0);
            DungeonTestData
                .FindPath(floor, floor.Entrance, floor.Rooms[1].Center)
                .Should()
                .NotBeNull();
        }
    }

    [Fact]
    public void SpawnMob_OnAFreeTile_AddsAnActiveMobThatPlaysWithTheOthers()
    {
        // Arrange
        (DungeonRun run, GeneratedDungeon dungeon) = StartSandbox();
        Position hero = run.HeroPosition;

        // Act
        run.SpawnMob(EnemyKind.Zombie, hero with { Y = hero.Y - 5 }, dungeon);
        run.SpawnMob(EnemyKind.Bat, hero with { Y = hero.Y + 5 }, dungeon);
        run.MoveHero(Direction.South, dungeon, MobRules.Default);

        // Assert
        run.Mobs.Select(mob => (mob.ElementId, mob.Kind))
            .Should()
            .Equal((0, EnemyKind.Zombie), (1, EnemyKind.Bat));
        run.Mobs.Should().OnlyContain(mob => mob.IsActive && mob.IsAggro);
        run.Turn.Should().Be(3);
    }

    [Fact]
    public void SpawnMob_OnTheHeroOrAWall_IsRefused()
    {
        // Arrange
        (DungeonRun run, GeneratedDungeon dungeon) = StartSandbox();

        // Act
        Action onHero = () => run.SpawnMob(EnemyKind.Mage, run.HeroPosition, dungeon);
        Action onWall = () => run.SpawnMob(EnemyKind.Mage, new Position(3, 3), dungeon);

        // Assert
        onHero.Should().Throw<InvalidSpawnException>();
        onWall.Should().Throw<InvalidSpawnException>();
        run.Mobs.Should().BeEmpty();
    }

    [Fact]
    public void SpawnMob_OutsideTheSandbox_IsRefused()
    {
        // Arrange
        GeneratedDungeon dungeon = DungeonTestData.Generate(DungeonTestData.ReferenceSeed);
        DungeonRun run = DungeonRun.Start(Guid.NewGuid(), Guid.NewGuid(), dungeon, Now);
        Position free = run.HeroPosition.Step(Direction.East);

        // Act
        Action spawn = () => run.SpawnMob(EnemyKind.Goblin, free, dungeon);

        // Assert
        spawn.Should().Throw<InvalidSpawnException>().WithMessage("*sandbox*");
    }

    [Fact]
    public void Generate_SandboxSeedOutsideDevelopment_IsAnOrdinaryDungeonWithoutSpawns()
    {
        // Arrange
        GeneratedDungeon dungeon = DungeonTestData.Generate(SandboxDungeon.Seed);
        DungeonRun run = DungeonRun.Start(Guid.NewGuid(), Guid.NewGuid(), dungeon, Now);

        // Act
        Action spawn = () =>
            run.SpawnMob(EnemyKind.Zombie, run.HeroPosition.Step(Direction.East), dungeon);

        // Assert
        dungeon.IsSandbox.Should().BeFalse();
        dungeon.RoomCount.Should().Be(DungeonSettings.DefaultRoomCount);
        spawn.Should().Throw<InvalidSpawnException>().WithMessage("*sandbox*");
    }
}
