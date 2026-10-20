using FluentValidation;

namespace Dungeon.Application.Features.DungeonRunUseCase.OpenChest;

public sealed class OpenChestCommandValidator : AbstractValidator<OpenChestCommand>
{
    public OpenChestCommandValidator()
    {
        RuleFor(command => command.RunId).NotEmpty().WithMessage("RunId is required.");
        RuleFor(command => command.ElementId).GreaterThanOrEqualTo(0);
    }
}
