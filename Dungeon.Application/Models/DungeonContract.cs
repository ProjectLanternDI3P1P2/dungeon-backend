using Dungeon.Domain.Entities;
using Dungeon.Domain.Enums;
using Dungeon.Domain.ValueObjects;

namespace Dungeon.Application.Models;

public sealed record PositionResult(int X, int Y)
{
    public static PositionResult From(Position position) => new(position.X, position.Y);
}

/// <param name="Kind">
/// How an enemy silhouette moves, or what a decoration shows; null for any other element.
/// </param>
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
            KindOf(element)
        );

    /// <summary>What an enemy is, or what a decoration shows; null for any other element.</summary>
    private static string? KindOf(DungeonElement element)
    {
        if (element.Kind is { } kind)
        {
            return DungeonContract.Name(kind);
        }

        return element.Decoration is { } decoration ? DungeonContract.Name(decoration) : null;
    }
}

/// <summary>A living mob of the current floor, where it stands this turn.</summary>
/// <param name="NextTeleport">Where a mage will land on its next action; null otherwise.</param>
/// <param name="InCombat">Engaged in the fight in progress: it does not play until it is settled.</param>
public sealed record MobResult(
    Guid Id,
    string Kind,
    int X,
    int Y,
    bool Aggro,
    PositionResult? NextTeleport,
    bool InCombat = false
)
{
    public static MobResult From(RunMob mob) =>
        new(
            mob.ElementId,
            DungeonContract.Name(mob.Kind),
            mob.X,
            mob.Y,
            mob.IsAggro,
            mob.Teleport is { } teleport ? PositionResult.From(teleport) : null,
            mob.InCombat
        );
}

/// <summary>The fight in progress, or the one settled on this turn.</summary>
/// <param name="Status">"pending", "active", "resolved" or "failed".</param>
/// <param name="Trigger">"collision" or "boss".</param>
/// <param name="Outcome">"won", "lost" or "fled" once resolved.</param>
public sealed record EncounterResult(
    Guid Id,
    string Status,
    string Trigger,
    Guid? CombatId,
    IReadOnlyList<Guid> MobIds,
    Guid? BossId,
    string? Outcome
)
{
    public static EncounterResult From(RunEncounter encounter) =>
        new(
            encounter.Id,
            DungeonContract.Name(encounter.Status),
            DungeonContract.Name(encounter.Trigger),
            encounter.CombatId,
            [.. encounter.MobElementIds],
            encounter.BossElementId,
            encounter.Outcome is { } outcome ? DungeonContract.Name(outcome) : null
        );
}

/// <summary>Hit points and mana a hero still has in the run; null: full (Player's values).</summary>
public sealed record HeroStateResult(Guid HeroId, int? CurrentHp, int? CurrentMana)
{
    public static HeroStateResult From(RunParticipant participant) =>
        new(participant.HeroId, participant.CurrentHp, participant.CurrentMana);
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
            CellType.Decoration => '*',
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
            CellType.Decoration => "decoration",
            _ => throw new ArgumentOutOfRangeException(nameof(cellType), cellType, null),
        };

    public static string Name(ElementType elementType) =>
        elementType switch
        {
            ElementType.Enemy => "enemy",
            ElementType.Boss => "boss",
            ElementType.Item => "item",
            ElementType.Trap => "trap",
            ElementType.Decoration => "decoration",
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
            RoomType.Secret => "secret",
            _ => throw new ArgumentOutOfRangeException(nameof(roomType), roomType, null),
        };

    public static string Name(DecorationKind decoration) =>
        decoration switch
        {
            DecorationKind.Ship => "ship",
            DecorationKind.Campfire => "campfire",
            DecorationKind.Strawberry => "strawberry",
            DecorationKind.GrassBlock => "grassBlock",
            DecorationKind.BluePortal => "bluePortal",
            DecorationKind.OrangePortal => "orangePortal",
            _ => throw new ArgumentOutOfRangeException(nameof(decoration), decoration, null),
        };

    public static string Name(EncounterStatus status) =>
        status switch
        {
            EncounterStatus.Pending => "pending",
            EncounterStatus.Active => "active",
            EncounterStatus.Resolved => "resolved",
            EncounterStatus.Failed => "failed",
            _ => throw new ArgumentOutOfRangeException(nameof(status), status, null),
        };

    public static string Name(EncounterTrigger trigger) =>
        trigger switch
        {
            EncounterTrigger.Collision => "collision",
            EncounterTrigger.Boss => "boss",
            _ => throw new ArgumentOutOfRangeException(nameof(trigger), trigger, null),
        };

    public static string Name(EncounterOutcome outcome) =>
        outcome switch
        {
            EncounterOutcome.Won => "won",
            EncounterOutcome.Lost => "lost",
            EncounterOutcome.Fled => "fled",
            _ => throw new ArgumentOutOfRangeException(nameof(outcome), outcome, null),
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

    /// <summary>Parses a mob kind sent by a client: zombie, mage, goblin or bat, any case.</summary>
    public static bool TryParseEnemyKind(string? value, out EnemyKind kind)
    {
        (bool parsed, kind) = value?.ToLowerInvariant() switch
        {
            "zombie" => (true, EnemyKind.Zombie),
            "mage" => (true, EnemyKind.Mage),
            "goblin" => (true, EnemyKind.Goblin),
            "bat" => (true, EnemyKind.Bat),
            _ => (false, default),
        };

        return parsed;
    }
}
