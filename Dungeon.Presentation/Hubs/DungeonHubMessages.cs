using Dungeon.Application.Features.DungeonRunUseCase.GetDungeonRunById;

namespace Dungeon.Presentation.Hubs;

/// <summary>Joins the group of a run and returns its current snapshot. Calling it again resynchronizes.</summary>
public sealed record JoinDungeonRequest(Guid DungeonId);

/// <param name="CommandId">Chosen by the client: a retry with the same id is not applied twice.</param>
/// <param name="Direction">north, east, south or west: one tile per command (turn-based).</param>
public sealed record MoveHeroRequest(Guid CommandId, Guid DungeonId, string Direction);

/// <summary>Debug only: a mob on a tile of the sandbox dungeon (seed <c>0000000SANDBX</c>).</summary>
/// <param name="Kind">zombie, mage, goblin or bat.</param>
public sealed record SpawnMobRequest(Guid CommandId, Guid DungeonId, string Kind, int X, int Y);

/// <summary>
/// The explicit answer of the Hub to every client call: accepted with the snapshot that
/// resulted, or refused with the reason and nothing changed.
/// </summary>
public sealed record DungeonCommandResult(
    Guid? CommandId,
    bool Accepted,
    DungeonCommandError? Error,
    GetDungeonRunByIdResult? Snapshot
)
{
    public static DungeonCommandResult Accept(Guid? commandId, GetDungeonRunByIdResult snapshot) =>
        new(commandId, true, null, snapshot);

    public static DungeonCommandResult Refuse(Guid? commandId, string code, string message) =>
        new(commandId, false, new DungeonCommandError(code, message), null);
}

/// <param name="Code">One of <see cref="DungeonCommandErrorCodes"/>.</param>
public sealed record DungeonCommandError(string Code, string Message);

/// <summary>The refusal reasons, mirroring the HTTP statuses of the REST API.</summary>
public static class DungeonCommandErrorCodes
{
    /// <summary>Malformed request (HTTP 422).</summary>
    public const string Validation = "validation";

    /// <summary>Unknown run (HTTP 404).</summary>
    public const string NotFound = "notFound";

    /// <summary>A game rule refused the command, e.g. a wall (HTTP 409).</summary>
    public const string Refused = "refused";

    /// <summary>Another command changed the run at the same time: resynchronize and retry (HTTP 409).</summary>
    public const string Conflict = "conflict";

    /// <summary>Unexpected server error (HTTP 500).</summary>
    public const string Internal = "internal";
}
