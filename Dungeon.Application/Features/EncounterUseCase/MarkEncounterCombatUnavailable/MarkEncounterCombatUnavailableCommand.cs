using Dungeon.Application.Abstractions;

namespace Dungeon.Application.Features.EncounterUseCase.MarkEncounterCombatUnavailable;

public sealed record MarkEncounterCombatUnavailableCommand(Guid RunId, Guid EncounterId) : ICommand;
