using Dungeon.Application.Features.DungeonRunUseCase.CreateDungeonRun;
using Dungeon.Application.Features.DungeonRunUseCase.DefeatFloorBoss;
using Dungeon.Application.Features.DungeonRunUseCase.GetDungeonRunByGameSessionId;
using Dungeon.Application.Features.DungeonRunUseCase.GetDungeonRunById;
using Dungeon.Presentation.DTO;
using Dungeon.Presentation.Hubs;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using ILogger = Serilog.ILogger;

namespace Dungeon.Presentation.Controllers;

/// <summary>Non-gameplay access to runs. Moving goes through <see cref="DungeonHub"/>.</summary>
[ApiController]
[Route("api/v1/dungeon-runs")]
public sealed class DungeonRunController(
    IMediator mediator,
    IHubContext<DungeonHub, IDungeonClient> dungeonHub,
    ILogger logger
) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateDungeonRunDto createDungeonRunDto,
        CancellationToken cancellationToken
    )
    {
        Guid runId = createDungeonRunDto.RunId ?? Guid.CreateVersion7();
        logger.Information(
            "Received request to create dungeon run {RunId} for game session {GameSessionId}.",
            runId,
            createDungeonRunDto.GameSessionId
        );

        await mediator.Send(
            new CreateDungeonRunCommand(
                runId,
                createDungeonRunDto.GameSessionId,
                createDungeonRunDto.Seed
            ),
            cancellationToken
        );
        // By game session: when the session already had its run, that run is the answer.
        var run = await mediator.Send(
            new GetDungeonRunByGameSessionIdQuery(createDungeonRunDto.GameSessionId),
            cancellationToken
        );

        logger.Information("Dungeon run {RunId} created with seed {Seed}.", run.Id, run.Seed);
        return CreatedAtAction(nameof(GetById), new { runId = run.Id }, run);
    }

    [HttpGet("{runId:guid}")]
    public async Task<IActionResult> GetById(Guid runId, CancellationToken cancellationToken)
    {
        logger.Information("Received request to get dungeon run {RunId}.", runId);

        var run = await mediator.Send(new GetDungeonRunByIdQuery(runId), cancellationToken);

        return Ok(run);
    }

    /// <summary>
    /// Records the defeat of the current floor's boss, which opens the gate down (or wins the
    /// run on the last floor). For Combat to call once the fight is won.
    /// 409 when the hero is not in the boss room.
    /// </summary>
    [HttpPost("{runId:guid}/boss-defeats")]
    public async Task<IActionResult> DefeatFloorBoss(
        Guid runId,
        CancellationToken cancellationToken
    )
    {
        logger.Information("Received boss defeat for dungeon run {RunId}.", runId);

        await mediator.Send(new DefeatFloorBossCommand(runId), cancellationToken);
        var run = await mediator.Send(new GetDungeonRunByIdQuery(runId), cancellationToken);

        // The players see the gate open without asking for it.
        await dungeonHub.Clients.Group(DungeonHub.GroupName(runId)).DungeonStateChanged(run);

        logger.Information(
            "Dungeon run {RunId}: boss of floor {Floor} defeated, status {Status}.",
            runId,
            run.CurrentFloor,
            run.Status
        );
        return Ok(run);
    }
}
