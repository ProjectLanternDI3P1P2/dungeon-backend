using Dungeon.Application.Features.EncounterUseCase.MarkEncounterCombatStarted;
using Dungeon.Application.Features.EncounterUseCase.MarkEncounterCombatUnavailable;
using Dungeon.Application.Ports;
using Dungeon.Domain.Entities;
using Dungeon.Domain.Enums;
using Dungeon.Domain.Repositories;
using MediatR;
using ILogger = Serilog.ILogger;

namespace Dungeon.Application.Services;

/// <summary>
/// Runs after the command that detected the contact has committed: no network call ever
/// happens inside a transaction. The encounter id is the idempotency key of CreateCombat,
/// so retrying (Hub RetryEncounter, next JoinDungeon) never opens a second fight. A failed
/// call leaves the party blocked in a failed encounter: never a victory by default.
/// </summary>
public sealed class EncounterCombatStarter(
    IDungeonRunRepository repository,
    ICombatGateway combatGateway,
    ISender sender,
    ILogger logger
) : IEncounterCombatStarter
{
    public async Task<bool> StartPendingAsync(Guid runId, CancellationToken cancellationToken)
    {
        DungeonRun? run = await repository.GetByIdAsync(runId, cancellationToken);
        if (
            run?.OpenEncounter is not RunEncounter encounter
            || encounter.Status is not (EncounterStatus.Pending or EncounterStatus.Failed)
        )
        {
            return true;
        }

        var request = new CombatEncounterRequest(
            encounter.Id,
            run.Id,
            run.GameSessionId,
            encounter.Floor + 1,
            encounter.RoomId ?? 0,
            encounter.Trigger == EncounterTrigger.Boss,
            [
                .. run.Participants.Select(participant => new CombatEncounterHero(
                    participant.HeroId,
                    participant.CurrentHp,
                    participant.CurrentMana
                )),
            ],
            [
                .. (
                    encounter.BossElementId is Guid boss
                        ? [new CombatEncounterMonster(boss, true)]
                        : Array.Empty<CombatEncounterMonster>()
                ),
                .. encounter.MobElementIds.Select(id => new CombatEncounterMonster(id, false)),
            ]
        );

        try
        {
            Guid combatId = await combatGateway.CreateCombatAsync(request, cancellationToken);
            await sender.Send(
                new MarkEncounterCombatStartedCommand(run.Id, encounter.Id, combatId),
                cancellationToken
            );
            logger.Information(
                "Encounter {EncounterId} of run {RunId} ({Trigger}, pack of {PackSize}) is combat {CombatId}.",
                encounter.Id,
                run.Id,
                encounter.Trigger,
                request.Monsters.Count,
                combatId
            );
            return true;
        }
        catch (CombatUnavailableException exception)
        {
            logger.Warning(
                exception,
                "Combat could not open the fight of encounter {EncounterId} of run {RunId}: the party waits for a retry.",
                encounter.Id,
                run.Id
            );
            await sender.Send(
                new MarkEncounterCombatUnavailableCommand(run.Id, encounter.Id),
                cancellationToken
            );
            return false;
        }
    }
}
