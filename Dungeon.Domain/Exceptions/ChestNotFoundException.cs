namespace Dungeon.Domain.Exceptions;

/// <summary>The requested element is not a chest of the run's current floor.</summary>
public sealed class ChestNotFoundException(Guid runId, int floor, int elementId)
    : DomainException(
        $"Chest element '{elementId}' was not found on floor {floor} of dungeon run '{runId}'."
    );
