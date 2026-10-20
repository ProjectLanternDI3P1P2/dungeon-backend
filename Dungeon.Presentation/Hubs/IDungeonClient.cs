using Dungeon.Application.Features.DungeonRunUseCase.GetDungeonRunById;

namespace Dungeon.Presentation.Hubs;

/// <summary>Events the Dungeon Hub sends to the game client.</summary>
public interface IDungeonClient
{
    /// <summary>
    /// The full, authoritative state of the run after an accepted change, sent to every
    /// connection of its group: the client replaces its state instead of patching it.
    /// </summary>
    Task DungeonStateChanged(GetDungeonRunByIdResult snapshot);
}
