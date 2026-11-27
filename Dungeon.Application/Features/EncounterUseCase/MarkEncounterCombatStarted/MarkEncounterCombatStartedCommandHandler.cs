using Dungeon.Application.Features.DungeonRunUseCase;
using Dungeon.Domain.Entities;
using Dungeon.Domain.Repositories;
using MediatR;

namespace Dungeon.Application.Features.EncounterUseCase.MarkEncounterCombatStarted;

public sealed class MarkEncounterCombatStartedCommandHandler(IDungeonRunRepository repository)
    : IRequestHandler<MarkEncounterCombatStartedCommand>
{
    public async Task Handle(
        MarkEncounterCombatStartedCommand request,
        CancellationToken cancellationToken
    )
    {
        DungeonRun run = await repository.GetRequiredAsync(request.RunId, cancellationToken);
        run.MarkEncounterCombatStarted(request.EncounterId, request.CombatId);
    }
}
