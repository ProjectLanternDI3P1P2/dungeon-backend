namespace Dungeon.Domain.Entities;

/// <summary>
/// Persistent state of a chest generated from a dungeon seed. The map itself stays
/// deterministic; only the fact that this run has emptied one of its chests is stored.
/// </summary>
public sealed class OpenedChest
{
    // Required by EF Core.
    private OpenedChest() { }

    internal OpenedChest(Guid dungeonRunId, int floor, int elementId, DateTimeOffset openedAt)
    {
        DungeonRunId = dungeonRunId;
        Floor = floor;
        ElementId = elementId;
        OpenedAt = openedAt;
    }

    public Guid DungeonRunId { get; private set; }

    /// <summary>Zero-based generated floor that contains the chest.</summary>
    public int Floor { get; private set; }

    /// <summary>Stable element identifier within the generated floor.</summary>
    public int ElementId { get; private set; }

    public DateTimeOffset OpenedAt { get; private set; }
}
