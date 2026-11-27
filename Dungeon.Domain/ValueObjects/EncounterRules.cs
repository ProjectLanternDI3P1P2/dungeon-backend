using Dungeon.Domain.Enums;

namespace Dungeon.Domain.ValueObjects;

/// <summary>How contacts become fights (configuration section <c>Dungeon:Encounter</c>).</summary>
/// <param name="EngageRadius">Chebyshev distance at which a mob joins the pack (diagonals included).</param>
/// <param name="MaxPackSize">Most mobs in one fight; the nearest are kept, then by element id.</param>
public sealed record EncounterRules(EncounterMode Mode, int EngageRadius, int MaxPackSize)
{
    public static EncounterRules Stub { get; } = new(EncounterMode.Stub, 1, 6);

    public EncounterRules Validate()
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(EngageRadius);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(MaxPackSize);
        return this;
    }
}
