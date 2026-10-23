namespace Dungeon.Application.Models;

/// <summary>Reward-owned loot returned synchronously when a dungeon chest is opened.</summary>
public sealed record ChestLoot(
    Guid RewardId,
    Guid DungeonRunId,
    Guid ChestId,
    int Floor,
    string Difficulty,
    bool AlreadyGenerated,
    IReadOnlyList<ChestLootItem> Items
);

/// <summary>One catalogue item selected by Rewards for a chest.</summary>
public sealed record ChestLootItem(Guid ItemId, string Name, string Rarity, int Quantity);
