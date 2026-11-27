using Dungeon.Application.Features.DungeonRunUseCase;
using Dungeon.Application.Models;
using Dungeon.Application.Ports;
using Dungeon.Domain.Entities;
using Dungeon.Domain.Repositories;
using MediatR;
using ILogger = Serilog.ILogger;

namespace Dungeon.Application.Features.EncounterUseCase.ResolveEncounter;

/// <returns>The run status after the fight: "active", "won" or "lost".</returns>
public sealed class ResolveEncounterCommandHandler(
    IDungeonRunRepository repository,
    IDungeonProvider dungeonProvider,
    ILogger logger
) : IRequestHandler<ResolveEncounterCommand, string>
{
    public async Task<string> Handle(
        ResolveEncounterCommand request,
        CancellationToken cancellationToken
    )
    {
        DungeonRun run = await repository.GetRequiredAsync(request.RunId, cancellationToken);
        run.ResolveEncounter(
            request.EncounterId,
            request.CombatId,
            request.Outcome,
            request.Heroes,
            request.DefeatedElementIds,
            dungeonProvider.Get(run)
        );

        logger.Information(
            "Encounter {EncounterId} of run {RunId} resolved by combat {CombatId}: {Outcome}, {DefeatedCount} defeated mob(s), run status {RunStatus}.",
            request.EncounterId,
            request.RunId,
            request.CombatId,
            request.Outcome,
            request.DefeatedElementIds.Count,
            run.Status
        );

        return DungeonContract.Name(run.Status);
    }
}
