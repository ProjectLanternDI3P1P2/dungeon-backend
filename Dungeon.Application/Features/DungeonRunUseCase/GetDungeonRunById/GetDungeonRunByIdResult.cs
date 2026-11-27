using Dungeon.Application.Models;
using Dungeon.Domain.Entities;
using Dungeon.Domain.Enums;
using Dungeon.Domain.ValueObjects;

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

    /// <summary>Item element IDs already opened on the current floor.</summary>
    public IReadOnlyList<Guid> OpenedChestElementIds { get; init; } = [];

    /// <summary>The rooms of the current floor the party has discovered: their mobs know it is here.</summary>
    public IReadOnlyList<int> DiscoveredRoomIds { get; init; } = [];

    /// <summary>
    /// The tiles of the current floor a hero has stood on, as a base64 bitset in row order
    /// (tile <c>i = y * width + x</c> is bit <c>i % 8</c> of byte <c>i / 8</c>): the client
    /// rebuilds its fog of war from it after a reload.
    /// </summary>
    public string VisitedTiles { get; init; } = string.Empty;

    public DateTimeOffset StartedAt { get; init; }

    /// <summary>The fight in progress (the party cannot move), or the one settled this turn; null otherwise.</summary>
    public EncounterResult? Encounter { get; init; }

    /// <summary>
    /// A contact opens a fight in Combat (Combat mode and a party); false: the historical
    /// stand-in, a contact kills the mob and the boss is defeated by the REST shortcut.
    /// </summary>
    public bool CombatEnabled { get; init; }

    /// <summary>The heroes of the party and the hit points and mana they have left.</summary>
    public IReadOnlyList<HeroStateResult> Heroes { get; init; } = [];

    /// <param name="dungeon">The dungeon of the run, rebuilt from its seed.</param>
    /// <param name="encounterRules">How contacts become fights; Stub when omitted.</param>
    public static GetDungeonRunByIdResult From(
        DungeonRun run,
        GeneratedDungeon dungeon,
        EncounterRules? encounterRules = null
    )
    {
        DungeonFloor floor = dungeon.Floors[run.CurrentFloor];

        return new GetDungeonRunByIdResult
        {
            Id = run.Id,
            GameSessionId = run.GameSessionId,
            Seed = run.Seed.ToString(),
            GeneratorVersion = run.GeneratorVersion,
            Status = DungeonContract.Name(run.Status),
            FloorCount = run.FloorCount,
            CurrentFloor = run.CurrentFloor,
            Hero = PositionResult.From(run.HeroPosition),
            Turn = run.Turn,
            FloorBossDefeated = run.IsFloorBossDefeated,
            CurrentRoomId = floor.GetRoomId(run.HeroPosition),
            ElementsHere = floor
                .GetElementsAt(run.HeroPosition)
                .Select(DungeonElementResult.From)
                .ToList(),
            Mobs = run.Mobs.Where(mob => mob.IsActive).Select(MobResult.From).ToList(),
            KilledMobs = run
                .Mobs.Where(mob => !mob.IsActive && mob.KilledOnTurn == run.Turn)
                .Select(MobResult.From)
                .ToList(),
            DiscoveredRoomIds = [.. run.DiscoveredRoomIds],
            VisitedTiles = Convert.ToBase64String(run.VisitedTiles.Span),
            OpenedChestElementIds = run
                .OpenedChests.Where(opened => opened.Floor == run.CurrentFloor)
                .Select(opened => opened.ElementId)
                .Order()
                .ToList(),
            StartedAt = run.StartedAt,
            Encounter = run.VisibleEncounter is { } encounter
                ? EncounterResult.From(encounter)
                : null,
            Heroes = [.. run.Participants.Select(HeroStateResult.From)],
            CombatEnabled =
                encounterRules is { Mode: EncounterMode.Combat } && run.Participants.Count > 0,
        };
    }
}
