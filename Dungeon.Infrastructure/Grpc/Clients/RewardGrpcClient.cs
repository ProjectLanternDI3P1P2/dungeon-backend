using Dungeon.Application.Models;
using Dungeon.Application.Ports;
using Dungeon.Infrastructure.Grpc.Configuration;
using Grpc.Core;
using Microsoft.Extensions.Options;
using Reward.Contracts.V1;

namespace Dungeon.Infrastructure.Grpc.Clients;

/// <summary>gRPC adapter for the Reward service's idempotent chest-loot operation.</summary>
public sealed class RewardGrpcClient(
    RewardLootService.RewardLootServiceClient client,
    IOptions<RewardGrpcClientOptions> options
) : IRewardClient
{
    public async Task<ChestLoot> GenerateChestLootAsync(
        Guid commandId,
        Guid dungeonRunId,
        Guid chestId,
        int floor,
        string difficulty,
        CancellationToken cancellationToken
    )
    {
        RewardGrpcClientOptions settings = options.Value;
        ChestLootReply reply = await client.GenerateChestLootAsync(
            new GenerateChestLootRequest
            {
                CommandId = commandId.ToString(),
                DungeonRunId = dungeonRunId.ToString(),
                ChestId = chestId.ToString(),
                Floor = floor,
                Difficulty = difficulty,
            },
            new CallOptions(
                deadline: DateTime.UtcNow.AddSeconds(settings.TimeoutSeconds),
                cancellationToken: cancellationToken
            )
        );

        return new ChestLoot(
            Guid.Parse(reply.RewardId),
            Guid.Parse(reply.DungeonRunId),
            Guid.Parse(reply.ChestId),
            reply.Floor,
            reply.Difficulty,
            reply.AlreadyGenerated,
            reply
                .Items.Select(item => new Dungeon.Application.Models.ChestLootItem(
                    Guid.Parse(item.ItemId),
                    item.Name,
                    item.Rarity,
                    item.Quantity
                ))
                .ToList()
        );
    }
}
