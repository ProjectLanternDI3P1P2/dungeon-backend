namespace Dungeon.Application.Ports;

/// <summary>Application port for the Combat service operation <c>CreateCombat</c>.</summary>
public interface ICombatGateway
{
    /// <returns>The id of the fight Combat opened (the same one for a retried request).</returns>
    /// <exception cref="CombatUnavailableException">Combat cannot be reached or refused the fight.</exception>
    Task<Guid> CreateCombatAsync(
        CombatEncounterRequest request,
        CancellationToken cancellationToken
    );
}

/// <param name="EncounterId">Idempotency key of the call (command id).</param>
/// <param name="Floor">1-based floor (Combat's convention).</param>
public sealed record CombatEncounterRequest(
    Guid EncounterId,
    Guid RunId,
    Guid GameSessionId,
    int Floor,
    int RoomId,
    bool IsBoss,
    IReadOnlyList<CombatEncounterHero> Heroes,
    IReadOnlyList<CombatEncounterMonster> Monsters
);

public sealed record CombatEncounterHero(Guid HeroId, int? CurrentHp, int? CurrentMana);

public sealed record CombatEncounterMonster(Guid ElementId, bool IsBoss);

/// <summary>Combat could not open the fight; the encounter waits for a retry.</summary>
public sealed class CombatUnavailableException(string message, Exception? innerException = null)
    : Exception(message, innerException);
