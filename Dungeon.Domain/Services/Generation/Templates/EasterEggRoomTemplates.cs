using Dungeon.Domain.Enums;

namespace Dungeon.Domain.Services.Generation.Templates;

/// <summary>
/// The easter-egg rooms: one tribute per room, to a game the class loves. Legendary, they
/// never come up by weight; a floor hides one only when <see cref="EasterEggs"/> rolls it,
/// and then one of them at random, so each is rarer still.
/// See <see cref="RoomTemplates"/> for the legend, and <see cref="RoomTemplate"/> for the
/// decoration letters.
/// <para>
/// To add one: draw its sprites in the client's easter-egg sheet, add a
/// <see cref="DecorationKind"/> and its letter, then a template here.
/// </para>
/// </summary>
internal static class EasterEggRoomTemplates
{
    public const string OuterWildsShip = "outerWildsShip";
    public const string OuterWildsCampfire = "outerWildsCampfire";
    public const string Celeste = "celeste";
    public const string Minecraft = "minecraft";
    public const string Portal = "portal";

    public static IReadOnlyList<RoomTemplate> All { get; } =
    [
        // Outer Wilds: the little ship has landed in an empty room.
        new(
            "Outer Wilds landing",
            [RoomType.Secret],
            [
                "...............",
                "...............",
                "..SSS..........",
                "..SSS..........",
                "...............",
                "...............",
                "...............",
                "...............",
                "...............",
                "...............",
                "...............",
            ],
            RoomRarity.Legendary,
            OuterWildsShip
        ),
        // Outer Wilds: a campfire burns alone, a marshmallow roasting over it.
        new(
            "Outer Wilds campfire",
            [RoomType.Secret],
            [
                "...........",
                "...........",
                "...........",
                "......C....",
                "...........",
                "...........",
                "...........",
                "...........",
                "...........",
            ],
            RoomRarity.Legendary,
            OuterWildsCampfire
        ),
        // Celeste: a winged strawberry floats in a quiet room, waiting to be caught.
        new(
            "Celeste summit",
            [RoomType.Secret],
            [
                "...........",
                "...........",
                ".....F.....",
                "...........",
                "...........",
                "...........",
                "...........",
                "...........",
                "...........",
            ],
            RoomRarity.Legendary,
            Celeste
        ),
        // Minecraft: a lone grass block, left in the middle of the dungeon.
        new(
            "Minecraft block",
            [RoomType.Secret],
            [
                "...........",
                "...........",
                "...........",
                "....M......",
                "...........",
                "...........",
                "...........",
                "...........",
                "...........",
            ],
            RoomRarity.Legendary,
            Minecraft
        ),
        // Portal: a test chamber, a blue and an orange portal on its back wall.
        new(
            "Portal test chamber",
            [RoomType.Secret],
            [
                "...P.....Q...",
                ".............",
                ".............",
                ".............",
                ".............",
                ".............",
                ".............",
                ".............",
                ".............",
            ],
            RoomRarity.Legendary,
            Portal
        ),
    ];
}
