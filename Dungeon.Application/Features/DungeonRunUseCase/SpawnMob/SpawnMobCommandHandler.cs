using Dungeon.Application.Models;
using Dungeon.Application.Ports;
using Dungeon.Domain.Entities;
using Dungeon.Domain.Enums;
using Dungeon.Domain.Repositories;
using Dungeon.Domain.ValueObjects;
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

        // The validator has already rejected unknown kinds (422).
        DungeonContract.TryParseEnemyKind(request.Kind, out EnemyKind kind);

        // Refused with InvalidSpawnException (409) outside the sandbox or on a taken tile.
        run.SpawnMob(kind, new Position(request.X, request.Y), dungeonProvider.Get(run));
    }
}
