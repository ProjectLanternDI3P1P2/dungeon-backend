using Dungeon.Domain.Entities;
using Dungeon.Domain.Enums;
using Dungeon.Domain.Services.Generation;
using Dungeon.Domain.ValueObjects;
using Dungeon.Test.TestSupport;
using FluentAssertions;

namespace Dungeon.Test.Domain.Services.Generation;

/// <summary>The legendary easter-egg rooms: rare, configurable, decorative only.</summary>
public class EasterEggsTests
{
    /// <summary>
    /// Seeds that hide each easter-egg room with the default odds, on the first floor: try
    /// them in the game. They stay valid as long as the generator and the odds do not change.
    /// </summary>
    public static TheoryData<string, string> SeedsWithAnEasterEgg() =>
        new()
        {
            { "0S8QMP5GDPJ11", "outerWildsShip" },
            { "0EANG6PJ9ZAV7", "outerWildsCampfire" },
            { "0XBWXVS90496E", "celeste" },
            { "01GWTHZB70BF7", "minecraft" },
            { "0CR57NSZKFK60", "portal" },
        };

    [Theory]
    [MemberData(nameof(DungeonTestData.FiftySeeds), MemberType = typeof(DungeonTestData))]
    public void Generate_OddsOfOne_HidesAnEasterEggOnEveryFloor(ulong seedValue)
    {
        // Act
        GeneratedDungeon dungeon = new DungeonGenerator(easterEggOneIn: 1).Generate(
            new Seed(seedValue),
            DungeonSettings.Default
        );

        // Assert: the validator ran inside Generate; every floor traded its empty room.
        foreach (DungeonFloor floor in dungeon.Floors)
        {
            Room secret = floor
                .Rooms.Should()
                .ContainSingle(room => room.Type == RoomType.Secret)
                .Subject;
            floor.Rooms.Should().NotContain(room => room.Type == RoomType.Empty);
            secret
                .EasterEgg.Should()
                .BeOneOf("outerWildsShip", "outerWildsCampfire", "celeste", "minecraft", "portal");

            List<DungeonElement> inside = floor.Elements.Where(e => e.RoomId == secret.Id).ToList();
            inside.Should().NotBeEmpty().And.OnlyContain(e => e.Type == ElementType.Decoration);
        }
    }

    [Theory]
    [MemberData(nameof(DungeonTestData.FiftySeeds), MemberType = typeof(DungeonTestData))]
    public void Generate_OddsOfZero_NeverHidesOne(ulong seedValue)
    {
        // Act
        GeneratedDungeon dungeon = new DungeonGenerator(easterEggOneIn: 0).Generate(
            new Seed(seedValue),
            DungeonSettings.Default
        );

        // Assert
        dungeon
            .Floors.SelectMany(floor => floor.Rooms)
            .Should()
            .NotContain(room => room.Type == RoomType.Secret);
    }

    [Theory]
    [MemberData(nameof(DungeonTestData.FiftySeeds), MemberType = typeof(DungeonTestData))]
    public void Generate_FloorWithoutAnEasterEgg_IsBuiltExactlyAsWithoutThem(ulong seedValue)
    {
        // Arrange
        Seed seed = new(seedValue);

        // Act
        GeneratedDungeon withEggs = new DungeonGenerator().Generate(seed, DungeonSettings.Default);
        GeneratedDungeon without = new DungeonGenerator(easterEggOneIn: 0).Generate(
            seed,
            DungeonSettings.Default
        );

        // Assert: the roll has its own stream; other floors are untouched.
        foreach ((DungeonFloor egg, DungeonFloor plain) in withEggs.Floors.Zip(without.Floors))
        {
            if (egg.Rooms.All(room => room.Type != RoomType.Secret))
            {
                (egg.Width, egg.Height).Should().Be((plain.Width, plain.Height));
                for (int y = 0; y < egg.Height; y++)
                {
                    for (int x = 0; x < egg.Width; x++)
                    {
                        egg.GetCell(new Position(x, y))
                            .Should()
                            .Be(plain.GetCell(new Position(x, y)));
                    }
                }

                egg.Elements.Should().BeEquivalentTo(plain.Elements);
            }
        }
    }

    [Theory]
    [MemberData(nameof(SeedsWithAnEasterEgg))]
    public void Generate_KnownSeed_HidesItsEasterEggOnTheFirstFloor(string seed, string easterEgg)
    {
        // Act
        GeneratedDungeon dungeon = new DungeonGenerator().Generate(
            Seed.Parse(seed),
            DungeonSettings.Default
        );

        // Assert
        dungeon
            .Floors[0]
            .Rooms.Should()
            .ContainSingle(room => room.Type == RoomType.Secret)
            .Which.EasterEgg.Should()
            .Be(easterEgg);
    }

    [Fact]
    public void Templates_AreLegendaryAndOnlyForSecretRooms()
    {
        List<RoomTemplate> eggs = RoomTemplates
            .All.Where(template => template.RoomTypes.Contains(RoomType.Secret))
            .ToList();

        eggs.Should().HaveCount(5);
        eggs.Should()
            .OnlyContain(template =>
                template.Rarity == RoomRarity.Legendary
                && template.RoomTypes.Count == 1
                && template.EasterEgg != null
            );
        RoomTemplates
            .All.Except(eggs)
            .Should()
            .OnlyContain(template => template.Rarity != RoomRarity.Legendary);
    }
}
