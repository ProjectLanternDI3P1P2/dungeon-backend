using Dungeon.Application.Models;
using Dungeon.Application.Ports;
using Dungeon.Domain.Entities;
using Dungeon.Domain.Repositories;
using MediatR;

namespace Dungeon.Application.Features.DungeonRunUseCase.GetDungeonRunHeroes;

public sealed class GetDungeonRunHeroesQueryHandler(
    IDungeonRunRepository dungeonRunRepository,
    IHeroAppearanceClient heroAppearanceClient
) : IRequestHandler<GetDungeonRunHeroesQuery, GetDungeonRunHeroesResult>
{
    public async Task<GetDungeonRunHeroesResult> Handle(
        GetDungeonRunHeroesQuery request,
        CancellationToken cancellationToken
    )
    {
        DungeonRun run = await dungeonRunRepository.GetRequiredAsync(
            request.RunId,
            cancellationToken
        );

        // Read from Player on every load and never stored: an appearance changed in the hero
        // editor shows the next time a run is loaded. The heroes of a party are read together.
        RunHeroResult[] heroes = await Task.WhenAll(
            run.HeroIds.Select(heroId => ReadHeroAsync(heroId, cancellationToken))
        );

        return new GetDungeonRunHeroesResult { RunId = run.Id, Heroes = heroes };
    }

    private async Task<RunHeroResult> ReadHeroAsync(
        Guid heroId,
        CancellationToken cancellationToken
    )
    {
        HeroAppearance? appearance = await heroAppearanceClient.GetAppearanceAsync(
            heroId,
            cancellationToken
        );

        return appearance is null
            ? new RunHeroResult(heroId, HeroAppearance.Default, IsDefaultAppearance: true)
            : new RunHeroResult(heroId, appearance, IsDefaultAppearance: false);
    }
}
