namespace Dungeon.Domain.Enums;

public enum EncounterOutcome
{
    Won,
    Lost,

    /// <summary>The party ran away: the mobs survive and skip their next turn.</summary>
    Fled,
}
