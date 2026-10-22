namespace Dungeon.Domain.Exceptions;

/// <summary>A chest can only be opened from its tile or one of its eight neighbours.</summary>
public sealed class ChestNotInReachException(Guid runId, int floor, Guid elementId)
    : DomainException(
        $"The hero of dungeon run '{runId}' is not within one tile of chest element '{elementId}' of floor {floor}."
    );
