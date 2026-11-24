using Dungeon.Application.Models;
using Dungeon.Application.Ports;
using Dungeon.Domain.Entities;
using Dungeon.Domain.Enums;
using Dungeon.Domain.Repositories;
using Dungeon.Domain.ValueObjects;
using FluentValidation;
using FluentValidation.Results;
using MediatR;

namespace Dungeon.Application.Features.DungeonRunUseCase.SpawnMob;

public sealed class SpawnMobCommandHandler(
    IDungeonRunRepository dungeonRunRepository,
    IDungeonProvider dungeonProvider
) : IRequestHandler<SpawnMobCommand>
{
    public async Task Handle(SpawnMobCommand request, CancellationToken cancellationToken)
    {
        DungeonRun run = await dungeonRunRepository.GetRequiredAsync(
            request.RunId,
            cancellationToken
        );

        // The validator has already rejected unknown kinds (422). A command that skipped it is
        // refused the same way, instead of spawning a mob of the default kind.
        if (!DungeonContract.TryParseEnemyKind(request.Kind, out EnemyKind kind))
        {
            throw new ValidationException([
                new ValidationFailure(nameof(request.Kind), SpawnMobValidator.KindMessage),
            ]);
        }

        // Refused with InvalidSpawnException (409) outside the sandbox or on a taken tile.
        run.SpawnMob(kind, new Position(request.X, request.Y), dungeonProvider.Get(run));
    }
}
