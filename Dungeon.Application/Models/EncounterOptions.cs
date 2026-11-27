using Dungeon.Domain.Enums;
using Dungeon.Domain.ValueObjects;

namespace Dungeon.Application.Models;

/// <summary>Configuration section <c>Dungeon:Encounter</c>: how contacts become fights.</summary>
public sealed class EncounterOptions
{
    public const string SectionName = "Dungeon:Encounter";

    /// <summary>
    /// <c>Stub</c>: a contact kills the mob (historical behaviour, no fight). <c>Combat</c>:
    /// a contact opens a fight in Combat with the whole pack.
    /// </summary>
    public EncounterMode Mode { get; init; } = EncounterMode.Stub;

    public int EngageRadius { get; init; } = 1;

    public int MaxPackSize { get; init; } = 6;

    public EncounterRules ToRules() =>
        new EncounterRules(Mode, EngageRadius, MaxPackSize).Validate();
}
