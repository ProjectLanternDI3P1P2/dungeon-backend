namespace Dungeon.Domain.Entities;

/// <summary>
/// A hero of the party exploring the run. Player owns the hero; the run only carries what
/// changes during the exploration: the hit points and mana left after a fight.
/// </summary>
public sealed class RunParticipant
{
    // Required by EF Core.
    private RunParticipant() { }

    public Guid HeroId { get; private set; }

    /// <summary>Null until the first fight: full, as Player says.</summary>
    public int? CurrentHp { get; private set; }

    /// <summary>Null until the first fight: full, as Player says.</summary>
    public int? CurrentMana { get; private set; }

    internal static RunParticipant Join(Guid heroId)
    {
        if (heroId == Guid.Empty)
        {
            throw new ArgumentException("A participant needs a hero id.", nameof(heroId));
        }

        return new RunParticipant { HeroId = heroId };
    }

    internal void Keep(int currentHp, int currentMana)
    {
        CurrentHp = Math.Max(0, currentHp);
        CurrentMana = Math.Max(0, currentMana);
    }
}
