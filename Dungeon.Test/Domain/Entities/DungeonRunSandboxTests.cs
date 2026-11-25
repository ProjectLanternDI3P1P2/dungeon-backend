using Dungeon.Domain.Entities;
using Dungeon.Domain.Enums;
using Dungeon.Domain.Exceptions;
using Dungeon.Domain.Mobs;
using Dungeon.Domain.Services.Generation;
using Dungeon.Domain.ValueObjects;
using Dungeon.Test.TestSupport;
using FluentAssertions;

namespace Dungeon.Test.Domain.Entities;

/// <summary>
/// The debug sandbox: two bare rooms where mobs are spawned by hand, then the easter-egg
/// rooms, in development only.
/// </summary>
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
    public void Generate_SandboxSeed_GivesTwoBareRoomsThenTheEasterEggRoomsInAColumn()
    {
        // Act
        GeneratedDungeon dungeon = GenerateSandbox();

        // Assert
        dungeon.Floors.Should().HaveCount(DungeonSettings.Default.FloorCount);
        foreach (DungeonFloor floor in dungeon.Floors)
        {
            floor.Rooms.Select(room => room.Id).Should().Equal(0, 1, 2, 3, 4, 5, 6);
            floor
                .Rooms.Select(room => room.Type)
                .Should()
                .Equal(
                    RoomType.Start,
                    RoomType.Empty,
                    RoomType.Secret,
                    RoomType.Secret,
                    RoomType.Secret,
                    RoomType.Secret,
                    RoomType.Secret
                );
            floor
                .Rooms.Select(room => room.EasterEgg)
                .Should()
                .Equal(
                    null,
                    null,
                    "outerWildsShip",
                    "outerWildsCampfire",
                    "celeste",
                    "minecraft",
                    "portal"
                );
            // Each room stands north of the one before it.
            floor
                .Rooms.Zip(floor.Rooms.Skip(1))
                .Should()
                .OnlyContain(pair => pair.Second.Interior.Bottom < pair.First.Interior.Y);
            DungeonTestData.PositionsOf(floor, CellType.Door).Should().HaveCount(12);
            floor.GetRoomId(floor.Entrance).Should().Be(0);
            floor
                .Rooms.Should()
                .OnlyContain(room =>
                    DungeonTestData.FindPath(floor, floor.Entrance, room.Center) != null
                );
            floor
                .Elements.Select(element => element.Decoration)
                .Should()
                .BeEquivalentTo([
                    DecorationKind.Ship,
                    DecorationKind.Campfire,
                    DecorationKind.Strawberry,
                    DecorationKind.GrassBlock,
                    DecorationKind.BluePortal,
                    DecorationKind.OrangePortal,
                ]);
            floor.Elements.Should().OnlyContain(element => element.Type == ElementType.Decoration);
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
        run.Mobs.Select(mob => mob.Kind)
            .Should()
            .BeEquivalentTo(new[] { EnemyKind.Zombie, EnemyKind.Bat });
        run.Mobs.Select(mob => mob.ElementId)
            .Should()
            .OnlyContain(id => id != Guid.Empty)
            .And.OnlyHaveUniqueItems();
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
