using Dungeon.Application.Abstractions;

namespace Dungeon.Application.Features.DungeonRunUseCase.SpawnMob;

/// <summary>Debug only: puts a mob on a tile of the sandbox dungeon, to watch how it moves.</summary>
/// <param name="Kind">zombie, mage, goblin or bat.</param>
public record SpawnMobCommand(Guid RunId, string Kind, int X, int Y) : ICommand;
