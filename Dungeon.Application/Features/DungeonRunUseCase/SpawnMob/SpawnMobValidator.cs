using Dungeon.Application.Models;
using FluentValidation;

namespace Dungeon.Application.Features.DungeonRunUseCase.SpawnMob;

public class SpawnMobValidator : AbstractValidator<SpawnMobCommand>
{
    public const string KindMessage = "Kind must be one of: zombie, mage, goblin, bat.";

    public SpawnMobValidator()
    {
        RuleFor(command => command.RunId).NotEmpty().WithMessage("RunId is required.");

        RuleFor(command => command.Kind)
            .Must(kind => DungeonContract.TryParseEnemyKind(kind, out _))
            .WithMessage(KindMessage);
    }
}
