using Dungeon.Application.Messaging;
using Dungeon.Application.Models;
using Dungeon.Application.Ports;
using Dungeon.Domain.Entities;
using Dungeon.Domain.Repositories;
using Dungeon.Domain.Services;
using MediatR;

namespace Dungeon.Application.Features.DungeonRunUseCase.OpenChest;

public sealed class OpenChestCommandHandler(
    IDungeonRunRepository dungeonRunRepository,
    IDungeonProvider dungeonProvider,
    IClock clock,
    IMessagePublisher messagePublisher,
    IRewardClient rewardClient
) : IRequestHandler<OpenChestCommand, ChestLoot>
{
    // TODO: derive this from the dungeon run once its difficulty is modelled.
    private const string DefaultRewardDifficulty = "NORMAL";

    public async Task<ChestLoot> Handle(
        OpenChestCommand request,
        CancellationToken cancellationToken
    )
    {
        DungeonRun run = await dungeonRunRepository.GetRequiredAsync(
            request.RunId,
            cancellationToken
        );
        DateTimeOffset openedAt = clock.UtcNow;

        bool opened = run.OpenChest(request.ElementId, dungeonProvider.Get(run), openedAt);
        int rewardFloor = run.CurrentFloor + 1;
        Guid rewardChestId = ChestRewardIdentity.For(run.Id, rewardFloor, request.ElementId);
        ChestLoot loot = await rewardClient.GenerateChestLootAsync(
            rewardChestId,
            run.Id,
            rewardChestId,
            rewardFloor,
            // Dungeon runs do not expose a difficulty yet; Rewards' local table uses NORMAL.
            DefaultRewardDifficulty,
            cancellationToken
        );

        if (opened)
        {
            // Rewards owns the contents; Dungeon only records the confirmed opening and exposes
            // the returned list to the caller. The state mutation is transactional with the
            // command, so a failed reward call or event publication rolls it back.
            await messagePublisher.PublishAsync(
                ChestOpenedMessageFactory.Create(
                    run.Id,
                    run.GameSessionId,
                    run.Seed.ToString(),
                    run.CurrentFloor,
                    request.ElementId,
                    openedAt
                ),
                cancellationToken
            );
        }

        return loot;
    }
}
