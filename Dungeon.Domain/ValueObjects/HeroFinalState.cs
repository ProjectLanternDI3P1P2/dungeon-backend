namespace Dungeon.Domain.ValueObjects;

/// <summary>A hero's state at the end of a fight, as Combat reports it.</summary>
public sealed record HeroFinalState(Guid HeroId, int CurrentHp, int CurrentMana, bool KnockedOut);
