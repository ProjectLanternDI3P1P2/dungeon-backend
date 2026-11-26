using Dungeon.Application.Ports;
using Dungeon.Domain.Entities;
using Dungeon.Domain.Repositories;
using Dungeon.Domain.ValueObjects;
using MediatR;

namespace Dungeon.Application.Features.DungeonRunUseCase.GetDungeonRunById;

public sealed class GetDungeonRunByIdQueryHandler(
    IDungeonRunRepository dungeonRunRepository,
    IDungeonProvider dungeonProvider,
    EncounterRules? encounterRules = null
) : IRequestHandler<GetDungeonRunByIdQuery, GetDungeonRunByIdResult>
{
    public async Task<GetDungeonRunByIdResult> Handle(
        GetDungeonRunByIdQuery request,
        CancellationToken cancellationToken
    )
    {
        DungeonRun run = await dungeonRunRepository.GetRequiredAsync(
            request.RunId,
            cancellationToken
        );

        return GetDungeonRunByIdResult.From(run, dungeonProvider.Get(run), encounterRules);
    }
}
