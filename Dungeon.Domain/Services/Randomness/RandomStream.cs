namespace Dungeon.Domain.Services.Randomness;

/// <summary>
/// Independent random streams derived from one seed. Each generation stage draws from its
/// own stream, so changing how many numbers one stage consumes (placing one more barrel,
/// say) never reshuffles the stages after it.
/// </summary>
public enum RandomStream : uint
{
    Layout = 1,

    // 2 was RoomShapes and 3 was Content (enemies and their loot), no longer drawn from:
    // never reuse them for another stage.
    RoomTemplates = 4,

    /// <summary>Where the enemy silhouettes stand, how many per room, and their kind.</summary>
    Mobs = 5,
}
