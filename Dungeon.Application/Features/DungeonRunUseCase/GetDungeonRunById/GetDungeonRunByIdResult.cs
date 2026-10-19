using Dungeon.Application.Models;

namespace Dungeon.Application.Features.DungeonRunUseCase.GetDungeonRunById;

public sealed class GetDungeonRunByIdResult
{
    public Guid Id { get; init; }
    public Guid GameSessionId { get; init; }
    public string Seed { get; init; } = string.Empty;
    public int GeneratorVersion { get; init; }
    public string Status { get; init; } = string.Empty;
    public int FloorCount { get; init; }
    public int CurrentFloor { get; init; }
    public PositionResult Hero { get; init; } = new(0, 0);
    public int Turn { get; init; }

    /// <summary>The boss of the current floor is defeated: the gate down is open.</summary>
    public bool FloorBossDefeated { get; init; }

    /// <summary>The room the hero stands in; null in a corridor or a doorway.</summary>
    public int? CurrentRoomId { get; init; }

    /// <summary>Elements on the hero's tile: an enemy there is where Combat takes over.</summary>
    public IReadOnlyList<DungeonElementResult> ElementsHere { get; init; } = [];

    /// <summary>The living mobs of the current floor, in element order.</summary>
    public IReadOnlyList<MobResult> Mobs { get; init; } = [];

    /// <summary>The mobs that died on this turn, on the tile they fell on.</summary>
    public IReadOnlyList<MobResult> KilledMobs { get; init; } = [];

    public DateTimeOffset StartedAt { get; init; }
}
