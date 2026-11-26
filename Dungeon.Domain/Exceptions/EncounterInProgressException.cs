namespace Dungeon.Domain.Exceptions;

/// <summary>The party is in a fight: no move until Combat reports the outcome.</summary>
public sealed class EncounterInProgressException(Guid runId, Guid encounterId)
    : DomainException($"Dungeon run '{runId}' is in combat (encounter '{encounterId}').")
{
    public Guid RunId { get; } = runId;
    public Guid EncounterId { get; } = encounterId;
}
