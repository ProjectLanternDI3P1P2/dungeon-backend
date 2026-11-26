using Dungeon.Application.Features.DungeonRunUseCase;
using Dungeon.Domain.Entities;
using Dungeon.Domain.Repositories;
using MediatR;

namespace Dungeon.Application.Features.EncounterUseCase.MarkEncounterCombatUnavailable;

public sealed class MarkEncounterCombatUnavailableCommandHandler(IDungeonRunRepository repository)
    : IRequestHandler<MarkEncounterCombatUnavailableCommand>
{
    public async Task Handle(
        MarkEncounterCombatUnavailableCommand request,
        CancellationToken cancellationToken
    )
    {
        DungeonRun run = await repository.GetRequiredAsync(request.RunId, cancellationToken);
        run.MarkEncounterCombatUnavailable(request.EncounterId);
    }
}
