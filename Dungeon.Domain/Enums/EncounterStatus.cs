namespace Dungeon.Domain.Enums;

/// <summary>Life of a contact between the party and mobs: detected, fought in Combat, settled.</summary>
public enum EncounterStatus
{
    /// <summary>Contact detected; Combat has not confirmed the fight yet.</summary>
    Pending,

    /// <summary>Combat opened the fight.</summary>
    Active,

    /// <summary>Combat reported the outcome.</summary>
    Resolved,

    /// <summary>Combat could not be reached; the party stays blocked until a retry succeeds.</summary>
    Failed,
}
