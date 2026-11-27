using Combat.Contracts.V1;
using Dungeon.Application.Ports;
using Dungeon.Infrastructure.Grpc.Configuration;
using Grpc.Core;
using Microsoft.Extensions.Options;

namespace Dungeon.Infrastructure.Grpc.Clients;

/// <summary>Opens the fight of an encounter in the Combat service (internal gRPC).</summary>
public sealed class CombatGrpcClient(
    CombatService.CombatServiceClient client,
    IOptions<CombatGrpcClientOptions> options
) : ICombatGateway
{
    public async Task<Guid> CreateCombatAsync(
        CombatEncounterRequest request,
        CancellationToken cancellationToken
    )
    {
        ArgumentNullException.ThrowIfNull(request);

        var message = new CreateCombatRequest
        {
            SessionId = request.GameSessionId.ToString(),
            EncounterId = request.EncounterId.ToString(),
            CommandId = request.EncounterId.ToString(),
            EncounterType = request.IsBoss ? EncounterType.Boss : EncounterType.Enemy,
            RunId = request.RunId.ToString(),
            Floor = request.Floor,
            RoomId = request.RoomId,
        };
        foreach (CombatEncounterHero hero in request.Heroes)
        {
            var participant = new EncounterParticipant { HeroId = hero.HeroId.ToString() };
            if (hero.CurrentHp is int hp)
            {
                participant.CurrentHp = hp;
            }

            if (hero.CurrentMana is int mana)
            {
                participant.CurrentMana = mana;
            }

            message.Participants.Add(participant);
        }

        foreach (CombatEncounterMonster monster in request.Monsters)
        {
            message.Monsters.Add(
                new EncounterMonster
                {
                    ElementId = monster.ElementId.ToString(),
                    IsBoss = monster.IsBoss,
                }
            );
        }

        try
        {
            CreateCombatResponse response = await client.CreateCombatAsync(
                message,
                new CallOptions(
                    deadline: DateTime.UtcNow.AddSeconds(options.Value.TimeoutSeconds),
                    cancellationToken: cancellationToken
                )
            );
            return Guid.Parse(response.CombatId);
        }
        catch (RpcException exception)
        {
            // Every refusal leaves the encounter waiting for a retry with the same key:
            // Combat or Player down, hero data missing, or a contract mismatch to fix.
            throw new CombatUnavailableException(
                $"Combat refused or could not open the fight ({exception.StatusCode}): {exception.Status.Detail}",
                exception
            );
        }
    }
}
