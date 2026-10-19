using Dungeon.Application.Models;
using Dungeon.Domain.Entities;

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

    public DateTimeOffset StartedAt { get; init; }

    /// <param name="dungeon">The dungeon of the run, rebuilt from its seed.</param>
    public static GetDungeonRunByIdResult From(DungeonRun run, GeneratedDungeon dungeon)
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
            StartedAt = run.StartedAt,
        };
    }
}
