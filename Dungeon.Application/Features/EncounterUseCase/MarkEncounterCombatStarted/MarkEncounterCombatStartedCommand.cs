using Dungeon.Application.Abstractions;

namespace Dungeon.Application.Features.EncounterUseCase.MarkEncounterCombatStarted;

public sealed record MarkEncounterCombatStartedCommand(Guid RunId, Guid EncounterId, Guid CombatId)
    : ICommand;
