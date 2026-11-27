using Dungeon.Application.Abstractions;
using Dungeon.Domain.Enums;
using Dungeon.Domain.ValueObjects;

namespace Dungeon.Application.Features.EncounterUseCase.ResolveEncounter;

/// <summary>Applies the outcome Combat reported. Idempotent on the combat id.</summary>
public sealed record ResolveEncounterCommand(
    Guid RunId,
    Guid EncounterId,
    Guid CombatId,
    EncounterOutcome Outcome,
    IReadOnlyList<HeroFinalState> Heroes,
    IReadOnlyList<Guid> DefeatedElementIds
) : ICommand<string>;
