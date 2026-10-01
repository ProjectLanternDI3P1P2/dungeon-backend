using Dungeon.Application.Features.DungeonRunUseCase.GetDungeonRunById;
using MediatR;

namespace Dungeon.Application.Features.DungeonRunUseCase.GetDungeonRunByGameSessionId;

/// <summary>The run of a Player game session, the same view as by run id.</summary>
public record GetDungeonRunByGameSessionIdQuery(Guid GameSessionId)
    : IRequest<GetDungeonRunByIdResult>;
