using Dungeon.Domain.Enums;

namespace Dungeon.Domain.Services.Generation;

/// <summary>
/// A hand-drawn room: one character per tile, see <see cref="RoomTemplates"/> for the legend.
/// </summary>
public sealed class RoomTemplate
{
    public const char FloorTile = '.';
    public const char WallTile = '#';
    public const char VoidTile = ' ';
    public const char ObstacleTile = 'o';
    public const char PillarTile = 'I';
    public const char FenceTile = '=';
    public const char GrateTile = 'g';
    public const char WaterTile = '~';
    public const char LavaTile = '^';
    public const char TombTile = 'T';
    public const char TrapSpot = 't';
    public const char TreasureSpot = '$';
    public const char BossSpot = 'B';

    // Easter-egg decorations. Each letter covers the decoration's whole footprint; the
    // element stands on its first tile in reading order. Portals hang on the north wall:
    // their spot, on the top row, stays walkable.
    public const char ShipSpot = 'S';
    public const char CampfireSpot = 'C';
    public const char StrawberrySpot = 'F';
    public const char GrassBlockSpot = 'M';
    public const char BluePortalSpot = 'P';
    public const char OrangePortalSpot = 'Q';

    /// <summary>Every decoration letter, in a fixed order: elements are placed in it.</summary>
    public static IReadOnlyList<char> DecorationSpots { get; } =
    [ShipSpot, CampfireSpot, StrawberrySpot, GrassBlockSpot, BluePortalSpot, OrangePortalSpot];

    public RoomTemplate(
        string name,
        IReadOnlyList<RoomType> roomTypes,
        IReadOnlyList<string> rows,
        RoomRarity rarity = RoomRarity.Common,
        string? easterEgg = null
    )
    {
        ArgumentOutOfRangeException.ThrowIfZero(rows.Count);

        Name = name;
        RoomTypes = roomTypes;
        Rows = rows;
        Rarity = rarity;
        EasterEgg = easterEgg;
        Width = rows[0].Length;
        Height = rows.Count;
    }

    public string Name { get; }

    /// <summary>The kinds of room this template can be used for.</summary>
    public IReadOnlyList<RoomType> RoomTypes { get; }

    public IReadOnlyList<string> Rows { get; }

    /// <summary>How often the template comes up among those that fit a room.</summary>
    public RoomRarity Rarity { get; }

    /// <summary>The easter egg a <see cref="RoomType.Secret"/> template shows; null otherwise.</summary>
    public string? EasterEgg { get; }

    public int Width { get; }

    public int Height { get; }

    /// <summary>The character at (x, y), read right to left when the template is mirrored.</summary>
    public char At(int x, int y, bool mirrored)
    {
        return Rows[y][mirrored ? Width - 1 - x : x];
    }

    /// <summary>What the tile is made of; the spots for elements are floor tiles.</summary>
    public static CellType CellTypeOf(char tile)
    {
        return tile switch
        {
            WallTile => CellType.Wall,
            VoidTile => CellType.Void,
            ObstacleTile => CellType.Obstacle,
            PillarTile => CellType.Pillar,
            FenceTile => CellType.Fence,
            GrateTile => CellType.Grate,
            WaterTile => CellType.Water,
            LavaTile => CellType.Lava,
            TombTile => CellType.Tomb,
            ShipSpot or CampfireSpot or StrawberrySpot or GrassBlockSpot => CellType.Decoration,
            FloorTile
            or TrapSpot
            or TreasureSpot
            or BossSpot
            or BluePortalSpot
            or OrangePortalSpot => CellType.Floor,
            _ => throw new ArgumentOutOfRangeException(
                nameof(tile),
                tile,
                "Unknown template tile."
            ),
        };
    }

    /// <summary>The decoration a spot letter stands for.</summary>
    public static DecorationKind DecorationOf(char spot)
    {
        return spot switch
        {
            ShipSpot => DecorationKind.Ship,
            CampfireSpot => DecorationKind.Campfire,
            StrawberrySpot => DecorationKind.Strawberry,
            GrassBlockSpot => DecorationKind.GrassBlock,
            BluePortalSpot => DecorationKind.BluePortal,
            OrangePortalSpot => DecorationKind.OrangePortal,
            _ => throw new ArgumentOutOfRangeException(nameof(spot), spot, "Not a decoration."),
        };
    }
}
