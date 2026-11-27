namespace Dungeon.Domain.Exceptions;

/// <summary>In Combat mode the boss is defeated in a fight, never by the stand-in shortcut.</summary>
public sealed class BossResolvedByCombatException(Guid runId)
    : DomainException(
        $"The boss of dungeon run '{runId}' is resolved by Combat: walk into it to fight."
    )
{
    public Guid RunId { get; } = runId;
}
