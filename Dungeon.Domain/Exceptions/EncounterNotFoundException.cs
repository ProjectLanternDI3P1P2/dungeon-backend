namespace Dungeon.Domain.Exceptions;

/// <summary>The encounter is not one of the run, or another fight is reported for it.</summary>
public sealed class EncounterNotFoundException(Guid runId, Guid encounterId, string reason)
    : DomainException($"Encounter '{encounterId}' of dungeon run '{runId}': {reason}")
{
    public Guid RunId { get; } = runId;
    public Guid EncounterId { get; } = encounterId;
}
