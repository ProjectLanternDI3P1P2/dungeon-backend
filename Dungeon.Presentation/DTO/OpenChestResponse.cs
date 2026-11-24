using Dungeon.Application.Features.DungeonRunUseCase.GetDungeonRunById;
using Dungeon.Application.Models;

namespace Dungeon.Presentation.DTO;

/// <summary>
/// Returns the authoritative Dungeon state and the immutable Reward snapshot for the chest.
/// </summary>
public sealed record OpenChestResponse(GetDungeonRunByIdResult Run, ChestLoot Loot);
