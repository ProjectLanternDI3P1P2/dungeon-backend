using Dungeon.Application.Features.DungeonRunUseCase.CreateDungeonRun;
using Dungeon.Application.Features.DungeonRunUseCase.GetDungeonRunByGameSessionId;
using Dungeon.Application.Features.DungeonRunUseCase.GetDungeonRunById;
using Dungeon.Contracts.V1;
using Grpc.Core;
using MediatR;
using ILogger = Serilog.ILogger;

namespace Dungeon.Presentation.Grpc.Services;

/// <summary>
/// Internal gRPC surface through which Player starts the run of a game session
/// (ADR-GLOB-011). Never exposed to clients: REST and SignalR stay the public APIs.
/// </summary>
public sealed class DungeonRunGrpcService(IMediator mediator, ILogger logger)
    : DungeonRunService.DungeonRunServiceBase
{
    public override async Task<CreateDungeonRunResponse> CreateDungeonRun(
        CreateDungeonRunRequest request,
        ServerCallContext context
    )
    {
        Guid commandId = ParseUuid(request.CommandId, "command_id");
        Guid gameSessionId = ParseUuid(request.GameSessionId, "game_session_id");
        IReadOnlyList<Guid> heroIds = ParseParticipants(request.Participants);
        string? seed = string.IsNullOrWhiteSpace(request.Seed) ? null : request.Seed;

        logger.Information(
            "gRPC request {CommandId} to start the run of game session {GameSessionId} with {HeroCount} heroes.",
            commandId,
            gameSessionId,
            heroIds.Count
        );

        // The command id doubles as the id of a new run: a retry is idempotent.
        await mediator.Send(
            new CreateDungeonRunCommand(commandId, gameSessionId, seed),
            context.CancellationToken
        );
        // Read back by game session: a session started earlier keeps its first run.
        GetDungeonRunByIdResult run = await mediator.Send(
            new GetDungeonRunByGameSessionIdQuery(gameSessionId),
            context.CancellationToken
        );

        logger.Information(
            "Game session {GameSessionId} explores dungeon run {RunId} with seed {Seed}.",
            gameSessionId,
            run.Id,
            run.Seed
        );

        return new CreateDungeonRunResponse { RunId = run.Id.ToString(), Seed = run.Seed };
    }

    private static Guid ParseUuid(string value, string field)
    {
        if (!Guid.TryParse(value, out Guid id) || id == Guid.Empty)
        {
            throw InvalidArgument($"{field} must be a valid, non-empty UUID.");
        }

        return id;
    }

    private static List<Guid> ParseParticipants(IEnumerable<DungeonRunParticipant> participants)
    {
        List<Guid> heroIds = participants
            .Select(participant => ParseUuid(participant.HeroId, "participants.hero_id"))
            .ToList();

        if (heroIds.Count == 0)
        {
            throw InvalidArgument("participants must hold at least one hero.");
        }

        if (heroIds.Distinct().Count() != heroIds.Count)
        {
            throw InvalidArgument("participants must list each hero once.");
        }

        return heroIds;
    }

    private static RpcException InvalidArgument(string detail) =>
        new(new Status(StatusCode.InvalidArgument, detail));
}
