using FluentValidation;

namespace Dungeon.Application.Features.EncounterUseCase.ResolveEncounter;

public sealed class ResolveEncounterValidator : AbstractValidator<ResolveEncounterCommand>
{
    public ResolveEncounterValidator()
    {
        RuleFor(command => command.RunId).NotEmpty();
        RuleFor(command => command.EncounterId).NotEmpty();
        RuleFor(command => command.CombatId).NotEmpty();
        RuleFor(command => command.Outcome).IsInEnum();
    }
}
