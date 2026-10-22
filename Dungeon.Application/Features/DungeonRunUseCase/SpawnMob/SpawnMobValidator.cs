using Dungeon.Application.Models;
using FluentValidation;

namespace Dungeon.Application.Features.DungeonRunUseCase.SpawnMob;

public class SpawnMobValidator : AbstractValidator<SpawnMobCommand>
{
    public SpawnMobValidator()
    {
        RuleFor(command => command.RunId).NotEmpty().WithMessage("RunId is required.");

        RuleFor(command => command.Kind)
            .Must(kind => DungeonContract.TryParseEnemyKind(kind, out _))
            .WithMessage("Kind must be one of: zombie, mage, goblin, bat.");
    }
}
