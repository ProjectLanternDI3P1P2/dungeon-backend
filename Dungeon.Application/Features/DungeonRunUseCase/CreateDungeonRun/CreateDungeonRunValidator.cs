using Dungeon.Application.Features.DungeonUseCase;
using FluentValidation;

namespace Dungeon.Application.Features.DungeonRunUseCase.CreateDungeonRun;

public class CreateDungeonRunValidator : AbstractValidator<CreateDungeonRunCommand>
{
    public CreateDungeonRunValidator()
    {
        RuleFor(command => command.RunId).NotEmpty().WithMessage("RunId is required.");

        RuleFor(command => command.GameSessionId)
            .NotEmpty()
            .WithMessage("GameSessionId is required.");

        RuleFor(command => command.Seed)
            .Must(SeedRules.BeAValidSeed)
            .When(command => command.Seed is not null)
            .WithMessage(SeedRules.InvalidSeedMessage);

        RuleForEach(command => command.HeroIds)
            .NotEmpty()
            .WithMessage("HeroIds must not contain an empty id.");

        RuleFor(command => command.HeroIds)
            .Must(heroIds => heroIds!.Distinct().Count() == heroIds!.Count)
            .When(command => command.HeroIds is not null)
            .WithMessage("HeroIds must list each hero once.");
    }
}
