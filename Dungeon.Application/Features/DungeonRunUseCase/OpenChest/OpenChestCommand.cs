using Dungeon.Application.Abstractions;
using Dungeon.Application.Models;

namespace Dungeon.Application.Features.DungeonRunUseCase.OpenChest;

/// <param name="ElementId">Stable ID of the item/chest on or next to the hero.</param>
public sealed record OpenChestCommand(Guid RunId, int ElementId) : ICommand<ChestLoot>;
