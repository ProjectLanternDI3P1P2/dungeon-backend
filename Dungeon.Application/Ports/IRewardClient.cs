using Dungeon.Application.Models;

namespace Dungeon.Application.Ports;

/// <summary>Application port for synchronous chest-loot generation owned by Rewards.</summary>
public interface IRewardClient
{
    Task<ChestLoot> GenerateChestLootAsync(
        Guid commandId,
        Guid dungeonRunId,
        Guid chestId,
        int floor,
        string difficulty,
        CancellationToken cancellationToken
    );
}
