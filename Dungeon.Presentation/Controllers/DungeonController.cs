using System.Security.Cryptography;
using System.Text.Json;
using Dungeon.Application.Features.DungeonUseCase.GetDungeonCell;
using Dungeon.Application.Features.DungeonUseCase.GetDungeonMap;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using ILogger = Serilog.ILogger;

namespace Dungeon.Presentation.Controllers;

/// <summary>
/// Read access to generated dungeons by seed, for the game client and for the Combat and
/// Inventory services (US-DUNGEON-02).
/// </summary>
[ApiController]
[Route("api/v1/dungeons")]
public sealed class DungeonController(IMediator mediator, ILogger logger) : ControllerBase
{
    // A seed gives the same floor only as long as the generator, its templates and its
    // settings stay the same: a new easter-egg room, other odds, and the floor of a seed
    // changes. So the browser keeps a floor but asks again before each use, sending its
    // ETag; an unchanged floor costs a 304 without a body, a changed one comes back whole.
    // The ETag hashes the content itself, so no change can slip past it. Weak, because the
    // body may be compressed. Error responses are not affected: they carry neither header.
    private const string RevalidatedCacheControl = "public, no-cache";

    private static readonly JsonSerializerOptions ETagJson = new(JsonSerializerDefaults.Web);

    [HttpGet("{seed}/map")]
    public async Task<IActionResult> GetMap(
        string seed,
        [FromQuery] int floor,
        CancellationToken cancellationToken
    )
    {
        logger.Information("Received request to get floor {Floor} of dungeon {Seed}.", floor, seed);

        var map = await mediator.Send(new GetDungeonMapQuery(seed, floor), cancellationToken);

        return Revalidated(map);
    }

    [HttpGet("{seed}/cell")]
    public async Task<IActionResult> GetCell(
        string seed,
        [FromQuery] int? x,
        [FromQuery] int? y,
        [FromQuery] int floor,
        CancellationToken cancellationToken
    )
    {
        logger.Information(
            "Received request to get cell ({X}, {Y}) of floor {Floor} of dungeon {Seed}.",
            x,
            y,
            floor,
            seed
        );

        var cell = await mediator.Send(
            new GetDungeonCellQuery(seed, floor, x, y),
            cancellationToken
        );

        return Revalidated(cell);
    }

    /// <summary>The body with its ETag, or 304 when the client already holds this very one.</summary>
    private IActionResult Revalidated<T>(T body)
    {
        string etag = ETagOf(body);
        Response.Headers.CacheControl = RevalidatedCacheControl;
        Response.Headers.ETag = etag;

        bool known = Request
            .Headers.IfNoneMatch.SelectMany(value => (value ?? string.Empty).Split(','))
            .Select(tag => tag.Trim())
            .Any(tag => tag == etag || tag == "*");
        return known ? StatusCode(StatusCodes.Status304NotModified) : Ok(body);
    }

    internal static string ETagOf<T>(T body)
    {
        byte[] hash = SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(body, ETagJson));
        return $"W/\"{Convert.ToHexString(hash, 0, 16)}\"";
    }
}
