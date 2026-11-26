namespace Dungeon.Domain.Enums;

/// <summary>What a contact with a mob does.</summary>
public enum EncounterMode
{
    /// <summary>Historical stand-in: a contact kills the mob, no fight.</summary>
    Stub,

    /// <summary>A contact opens a fight in Combat with the whole pack.</summary>
    Combat,
}
