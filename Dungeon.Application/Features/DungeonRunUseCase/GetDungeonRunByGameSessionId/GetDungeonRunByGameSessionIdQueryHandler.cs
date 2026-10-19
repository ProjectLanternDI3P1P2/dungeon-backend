using Dungeon.Application.Features.DungeonRunUseCase.GetDungeonRunById;
using Dungeon.Application.Ports;
using Dungeon.Domain.Entities;
using Dungeon.Domain.Repositories;
using MediatR;

namespace Dungeon.Application.Features.DungeonRunUseCase.GetDungeonRunByGameSessionId;

public sealed class GetDungeonRunByGameSessionIdQueryHandler(
    IDungeonRunRepository dungeonRunRepository,
    IDungeonProvider dungeonProvider
) : IRequestHandler<GetDungeonRunByGameSessionIdQuery, GetDungeonRunByIdResult>
{
    public async Task<GetDungeonRunByIdResult> Handle(
        GetDungeonRunByGameSessionIdQuery request,
        CancellationToken cancellationToken
    )
    {
        DungeonRun run =
            await dungeonRunRepository.FindByGameSessionIdAsync(
                request.GameSessionId,
                cancellationToken
            )
            ?? throw new KeyNotFoundException(
                $"Dungeon run not found for GameSessionId '{request.GameSessionId}'."
            );

        return GetDungeonRunByIdResult.From(run, dungeonProvider.Get(run));
    }
}
