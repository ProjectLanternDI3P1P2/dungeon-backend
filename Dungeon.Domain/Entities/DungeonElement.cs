using Dungeon.Domain.Enums;
using Dungeon.Domain.ValueObjects;

namespace Dungeon.Domain.Entities;

/// <summary>
/// An abstract element placed by the generator. <see cref="Id"/> is stable for a given
/// seed and floor, so other services can reference it. <see cref="Kind"/> is set on
/// enemies only.
/// </summary>
public sealed record DungeonElement(
    Guid Id,
    ElementType Type,
    Position Position,
    int RoomId,
    EnemyKind? Kind = null
);
