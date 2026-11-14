using Dungeon.Application.Models;
using FluentValidation;

namespace Dungeon.Application.Features.DungeonRunUseCase.MoveHero;

public class MoveHeroValidator : AbstractValidator<MoveHeroCommand>
{
    public const string DirectionMessage = "Direction must be one of: north, east, south, west.";

    public MoveHeroValidator()
    {
        RuleFor(command => command.RunId).NotEmpty().WithMessage("RunId is required.");

        RuleFor(command => command.Direction)
            .Must(direction => DungeonContract.TryParseDirection(direction, out _))
            .WithMessage(DirectionMessage);
    }
}
