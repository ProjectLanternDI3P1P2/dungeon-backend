namespace Dungeon.Domain.Enums;

public enum EncounterTrigger
{
    /// <summary>The hero touched one or more mobs.</summary>
    Collision,

    /// <summary>The hero walked into the boss of the floor.</summary>
    Boss,
}
