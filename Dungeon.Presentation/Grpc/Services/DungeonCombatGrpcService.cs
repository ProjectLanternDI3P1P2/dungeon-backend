using Dungeon.Application.Features.DungeonRunUseCase.GetDungeonRunById;
using Dungeon.Application.Features.EncounterUseCase.ResolveEncounter;
using Dungeon.Contracts.V1;
using Dungeon.Domain.Enums;
using Dungeon.Presentation.Hubs;
using Grpc.Core;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using DomainHeroFinalState = Dungeon.Domain.ValueObjects.HeroFinalState;
using ILogger = Serilog.ILogger;

namespace Dungeon.Presentation.Grpc.Services;

/// <summary>
/// Combat reports here, on the internal gRPC endpoint only, the outcome of a fight Dungeon
/// opened. The run is updated, then every client of the run receives the new snapshot.
/// </summary>
public sealed class DungeonCombatGrpcService(
    IMediator mediator,
    IHubContext<DungeonHub, IDungeonClient> dungeonHub,
    ILogger logger
) : DungeonCombatService.DungeonCombatServiceBase
{
    public override async Task<ReportCombatResultResponse> ReportCombatResult(
        ReportCombatResultRequest request,
        ServerCallContext context
    )
    {
        Guid commandId = ParseUuid(request.CommandId, "command_id");
        Guid combatId = ParseUuid(request.CombatId, "combat_id");
        Guid runId = ParseUuid(request.RunId, "run_id");
        Guid encounterId = ParseUuid(request.EncounterId, "encounter_id");
        EncounterOutcome outcome = request.Outcome switch
        {
            CombatOutcome.Won => EncounterOutcome.Won,
            CombatOutcome.Lost => EncounterOutcome.Lost,
            CombatOutcome.Fled => EncounterOutcome.Fled,
            _ => throw InvalidArgument("outcome must be WON, LOST or FLED."),
        };
        List<DomainHeroFinalState> heroes =
        [
            .. request.Heroes.Select(hero => new DomainHeroFinalState(
                ParseUuid(hero.HeroId, "heroes.hero_id"),
                hero.CurrentHp,
                hero.CurrentMana,
                hero.KnockedOut
            )),
        ];
        List<Guid> defeated =
        [
            .. request.DefeatedElementIds.Select(id => ParseUuid(id, "defeated_element_ids")),
        ];

        logger.Information(
            "gRPC request {CommandId}: combat {CombatId} of run {RunId}, encounter {EncounterId} ended {Outcome}.",
            commandId,
            combatId,
            runId,
            encounterId,
            outcome
        );

        string runStatus = await mediator.Send(
            new ResolveEncounterCommand(runId, encounterId, combatId, outcome, heroes, defeated),
            context.CancellationToken
        );

        GetDungeonRunByIdResult snapshot = await mediator.Send(
            new GetDungeonRunByIdQuery(runId),
            context.CancellationToken
        );
        await dungeonHub.Clients.Group(DungeonHub.GroupName(runId)).DungeonStateChanged(snapshot);

        return new ReportCombatResultResponse { RunStatus = runStatus };
    }

    private static Guid ParseUuid(string value, string field)
    {
        if (!Guid.TryParse(value, out Guid id) || id == Guid.Empty)
        {
            throw InvalidArgument($"{field} must be a valid, non-empty UUID.");
        }

        return id;
    }

    private static RpcException InvalidArgument(string detail) =>
        new(new Status(StatusCode.InvalidArgument, detail));
}
