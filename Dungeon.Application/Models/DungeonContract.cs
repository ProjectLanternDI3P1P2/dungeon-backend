using Dungeon.Domain.Entities;
using Dungeon.Domain.Enums;
using Dungeon.Domain.ValueObjects;

namespace Dungeon.Application.Models;

public sealed record PositionResult(int X, int Y)
{
    public static PositionResult From(Position position) => new(position.X, position.Y);
}

/// <param name="Kind">How an enemy silhouette moves; null for any other element.</param>
public sealed record DungeonElementResult(
    Guid Id,
    string Type,
    int X,
    int Y,
    int RoomId,
    string? Kind
)
{
    public static DungeonElementResult From(DungeonElement element) =>
        new(
            element.Id,
            DungeonContract.Name(element.Type),
            element.Position.X,
            element.Position.Y,
            element.RoomId,
            element.Kind is { } kind ? DungeonContract.Name(kind) : null
        );
}

/// <summary>A living mob of the current floor, where it stands this turn.</summary>
/// <param name="NextTeleport">Where a mage will land on its next action; null otherwise.</param>
public sealed record MobResult(
    Guid Id,
    string Kind,
    int X,
    int Y,
    bool Aggro,
    PositionResult? NextTeleport
)
{
    public static MobResult From(RunMob mob) =>
        new(
            mob.ElementId,
            DungeonContract.Name(mob.Kind),
            mob.X,
            mob.Y,
            mob.IsAggro,
            mob.Teleport is { } teleport ? PositionResult.From(teleport) : null
        );
}

/// <summary>
/// Names used on the wire for the dungeon enums, written out by hand (ADR-0033) so that
/// renaming a C# member never breaks the Gateway, the frontend or another service.
/// </summary>
public static class DungeonContract
{
    /// <summary>One character per cell type, used to send a floor as rows of text.</summary>
    public static char Symbol(CellType cellType) =>
        cellType switch
        {
            CellType.Void => ' ',
            CellType.Floor => '.',
            CellType.Wall => '#',
            CellType.Door => '+',
            CellType.Obstacle => 'o',
            CellType.Pillar => 'I',
            CellType.Fence => '=',
            CellType.Gate => 'G',
            CellType.Grate => 'g',
            CellType.Water => '~',
            CellType.Lava => '^',
            CellType.Tomb => 'T',
            _ => throw new ArgumentOutOfRangeException(nameof(cellType), cellType, null),
        };

    public static string Name(CellType cellType) =>
        cellType switch
        {
            CellType.Void => "void",
            CellType.Floor => "floor",
            CellType.Wall => "wall",
            CellType.Door => "door",
            CellType.Obstacle => "obstacle",
            CellType.Pillar => "pillar",
            CellType.Fence => "fence",
            CellType.Gate => "gate",
            CellType.Grate => "grate",
            CellType.Water => "water",
            CellType.Lava => "lava",
            CellType.Tomb => "tomb",
            _ => throw new ArgumentOutOfRangeException(nameof(cellType), cellType, null),
        };

    public static string Name(ElementType elementType) =>
        elementType switch
        {
            ElementType.Enemy => "enemy",
            ElementType.Boss => "boss",
            ElementType.Item => "item",
            ElementType.Trap => "trap",
            _ => throw new ArgumentOutOfRangeException(nameof(elementType), elementType, null),
        };

    public static string Name(EnemyKind enemyKind) =>
        enemyKind switch
        {
            EnemyKind.Zombie => "zombie",
            EnemyKind.Mage => "mage",
            EnemyKind.Goblin => "goblin",
            EnemyKind.Bat => "bat",
            _ => throw new ArgumentOutOfRangeException(nameof(enemyKind), enemyKind, null),
        };

    public static string Name(RoomType roomType) =>
        roomType switch
        {
            RoomType.Start => "start",
            RoomType.Combat => "combat",
            RoomType.Treasure => "treasure",
            RoomType.Empty => "empty",
            RoomType.Boss => "boss",
            _ => throw new ArgumentOutOfRangeException(nameof(roomType), roomType, null),
        };

    public static string Name(DungeonRunStatus status) =>
        status switch
        {
            DungeonRunStatus.Active => "active",
            DungeonRunStatus.Won => "won",
            DungeonRunStatus.Lost => "lost",
            DungeonRunStatus.Abandoned => "abandoned",
            _ => throw new ArgumentOutOfRangeException(nameof(status), status, null),
        };

    /// <summary>Symbol to cell type name, sent with every map so that clients never hard-code it.</summary>
    public static IReadOnlyDictionary<string, string> Legend { get; } =
        Enum.GetValues<CellType>().ToDictionary(type => Symbol(type).ToString(), Name);

    /// <summary>Parses a direction sent by a client: north, east, south or west, any case.</summary>
    public static bool TryParseDirection(string? value, out Direction direction)
    {
        (bool parsed, direction) = value?.ToLowerInvariant() switch
        {
            "north" => (true, Direction.North),
            "east" => (true, Direction.East),
            "south" => (true, Direction.South),
            "west" => (true, Direction.West),
            _ => (false, default),
        };

        return parsed;
    }
}
