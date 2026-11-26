namespace Dungeon.Application.Services;

/// <summary>Opens in Combat the fight of a run's pending (or failed) encounter, outside any transaction.</summary>
public interface IEncounterCombatStarter
{
    /// <returns>True when the run has no encounter waiting, or Combat opened its fight.</returns>
    Task<bool> StartPendingAsync(Guid runId, CancellationToken cancellationToken);
}
