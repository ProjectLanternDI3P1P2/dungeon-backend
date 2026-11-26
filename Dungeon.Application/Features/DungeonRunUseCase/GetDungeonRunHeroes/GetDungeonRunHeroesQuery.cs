using MediatR;

namespace Dungeon.Application.Features.DungeonRunUseCase.GetDungeonRunHeroes;

/// <summary>The heroes of a run and how they look, read from Player when the run is loaded.</summary>
public sealed record GetDungeonRunHeroesQuery(Guid RunId) : IRequest<GetDungeonRunHeroesResult>;
