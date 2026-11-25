extern alias PlayerContracts;
using Dungeon.Application.Models;
using Dungeon.Application.Ports;
using Dungeon.Infrastructure.Grpc.Configuration;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PlayerContracts::Player.Contracts.V1;

namespace Dungeon.Infrastructure.Grpc.Clients;

/// <summary>
/// gRPC adapter reading a hero's appearance from Player (<c>PlayerHeroService</c>). Any
/// failure is answered by null, never by an exception: an appearance is cosmetic, and a run
/// must load even when Player cannot describe its heroes.
/// </summary>
public sealed class PlayerHeroGrpcClient(
    PlayerHeroService.PlayerHeroServiceClient client,
    IOptions<PlayerGrpcClientOptions> options,
    ILogger<PlayerHeroGrpcClient> logger
) : IHeroAppearanceClient
{
    public async Task<HeroAppearance?> GetAppearanceAsync(
        Guid heroId,
        CancellationToken cancellationToken
    )
    {
        HeroAppearanceReply reply;
        try
        {
            reply = await client.GetHeroAppearanceAsync(
                new GetHeroAppearanceRequest { HeroId = heroId.ToString() },
                new CallOptions(
                    deadline: DateTime.UtcNow.AddSeconds(options.Value.TimeoutSeconds),
                    cancellationToken: cancellationToken
                )
            );
        }
        // A cancelled request is not a Player failure: let it end the request.
        catch (RpcException exception) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning(
                "Player could not give the appearance of hero {HeroId} ({StatusCode}: {Detail}).",
                heroId,
                exception.StatusCode,
                exception.Status.Detail
            );
            return null;
        }

        if (!IsUsable(reply, heroId))
        {
            logger.LogWarning(
                "Player gave an unusable appearance for hero {HeroId}: {Reply}.",
                heroId,
                reply
            );
            return null;
        }

        return new HeroAppearance(
            reply.SkinTone,
            reply.HairStyle,
            reply.HasHairColor ? reply.HairColor : null
        );
    }

    /// <summary>
    /// Only what Dungeon needs to pass the codes on: the reply is about this hero and names a
    /// skin and a hair style. Whether a code exists in the catalogue is Player's rule.
    /// </summary>
    private static bool IsUsable(HeroAppearanceReply reply, Guid heroId) =>
        Guid.TryParse(reply.HeroId, out Guid repliedHeroId)
        && repliedHeroId == heroId
        && reply.SkinTone > 0
        && !string.IsNullOrWhiteSpace(reply.HairStyle);
}
