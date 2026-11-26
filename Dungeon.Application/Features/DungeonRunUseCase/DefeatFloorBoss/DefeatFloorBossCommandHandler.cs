using Dungeon.Application.Ports;
using Dungeon.Domain.Entities;
using Dungeon.Domain.Enums;
using Dungeon.Domain.Exceptions;
using Dungeon.Domain.Repositories;
using Dungeon.Domain.ValueObjects;
using MediatR;

namespace Dungeon.Application.Features.DungeonRunUseCase.DefeatFloorBoss;

public sealed class DefeatFloorBossCommandHandler(
    IDungeonRunRepository dungeonRunRepository,
    IDungeonProvider dungeonProvider,
    EncounterRules encounterRules
) : IRequestHandler<DefeatFloorBossCommand>
{
    public async Task Handle(DefeatFloorBossCommand request, CancellationToken cancellationToken)
    {
        DungeonRun run = await dungeonRunRepository.GetRequiredAsync(
            request.RunId,
            cancellationToken
        );

        // In Combat mode the boss is fought in Combat: this stand-in shortcut is refused (409).
        if (encounterRules.Mode == EncounterMode.Combat && run.Participants.Count > 0)
        {
            throw new BossResolvedByCombatException(run.Id);
        }

        // Refused with BossNotInReachException (409) when the hero is not in the boss room.
        run.DefeatFloorBoss(dungeonProvider.Get(run));
    }
}
