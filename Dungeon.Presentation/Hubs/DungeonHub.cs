using Dungeon.Application.Abstractions;
using Dungeon.Application.Features.DungeonRunUseCase.GetDungeonRunById;
using Dungeon.Application.Features.DungeonRunUseCase.MoveHero;
using Dungeon.Domain.Exceptions;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using ILogger = Serilog.ILogger;

namespace Dungeon.Presentation.Hubs;

/// <summary>
/// Gameplay of a dungeon run over SignalR, the same commands for solo and multiplayer
/// (ADR-GLOB-010). Every call answers with an explicit <see cref="DungeonCommandResult"/>, and
/// every accepted change sends the full snapshot to the group <c>dungeon:{DungeonId}</c>.
/// </summary>
/// <remarks>
/// Not yet authorized per participant: any connection may join a run it knows the id of.
/// Restricting the group to the roster of the game session comes with multiplayer.
/// </remarks>
public sealed class DungeonHub(IMediator mediator, HubCommandCache commandCache, ILogger logger)
    : Hub<IDungeonClient>
{
    public const string Route = "/hubs/dungeons";

    /// <summary>Set by the API Gateway from the Keycloak token (ADR-GLOB-004).</summary>
    private const string UserIdHeader = "X-User-Id";

    public static string GroupName(Guid dungeonId) => $"dungeon:{dungeonId}";

    public override Task OnConnectedAsync()
    {
        logger.Information(
            "Connection {ConnectionId} of user {UserId} opened on the Dungeon Hub.",
            Context.ConnectionId,
            GetUserId()
        );
        return base.OnConnectedAsync();
    }

    public async Task<DungeonCommandResult> JoinDungeon(JoinDungeonRequest request)
    {
        logger.Information(
            "Connection {ConnectionId} joins dungeon run {DungeonId}.",
            Context.ConnectionId,
            request.DungeonId
        );

        return await RefuseOnErrorAsync(
            commandId: null,
            async () =>
            {
                GetDungeonRunByIdResult snapshot = await mediator.Send(
                    new GetDungeonRunByIdQuery(request.DungeonId),
                    Context.ConnectionAborted
                );
                // Only once the run is known, so that a refused join leaves no group behind.
                await Groups.AddToGroupAsync(
                    Context.ConnectionId,
                    GroupName(request.DungeonId),
                    Context.ConnectionAborted
                );
                return DungeonCommandResult.Accept(null, snapshot);
            }
        );
    }

    public Task<DungeonCommandResult> MoveHero(MoveHeroRequest request)
    {
        logger.Information(
            "Command {CommandId}: move the hero of dungeon run {DungeonId} {Direction}.",
            request.CommandId,
            request.DungeonId,
            request.Direction
        );

        return ExecuteAsync(
            request.CommandId,
            request.DungeonId,
            new MoveHeroCommand(request.DungeonId, request.Direction)
        );
    }

    private async Task<DungeonCommandResult> ExecuteAsync<TCommand>(
        Guid commandId,
        Guid dungeonId,
        TCommand command
    )
        where TCommand : ICommand
    {
        if (commandId == Guid.Empty)
        {
            return DungeonCommandResult.Refuse(
                null,
                DungeonCommandErrorCodes.Validation,
                "CommandId is required."
            );
        }

        return await commandCache.RunOnceAsync(
            commandId,
            () =>
                RefuseOnErrorAsync(
                    commandId,
                    async () =>
                    {
                        await mediator.Send(command, Context.ConnectionAborted);
                        GetDungeonRunByIdResult snapshot = await mediator.Send(
                            new GetDungeonRunByIdQuery(dungeonId),
                            Context.ConnectionAborted
                        );

                        await Clients.Group(GroupName(dungeonId)).DungeonStateChanged(snapshot);
                        return DungeonCommandResult.Accept(commandId, snapshot);
                    }
                )
        );
    }

    /// <summary>Turns the errors <c>ExceptionHandlingMiddleware</c> maps for HTTP into explicit refusals.</summary>
    private async Task<DungeonCommandResult> RefuseOnErrorAsync(
        Guid? commandId,
        Func<Task<DungeonCommandResult>> action
    )
    {
        try
        {
            return await action();
        }
        catch (ValidationException exception)
        {
            logger.Warning(exception, "Hub command {CommandId} is invalid.", commandId);
            return DungeonCommandResult.Refuse(
                commandId,
                DungeonCommandErrorCodes.Validation,
                string.Join(" ", exception.Errors.Select(error => error.ErrorMessage))
            );
        }
        catch (KeyNotFoundException exception)
        {
            logger.Warning(exception, "Hub command {CommandId}: resource not found.", commandId);
            return DungeonCommandResult.Refuse(
                commandId,
                DungeonCommandErrorCodes.NotFound,
                exception.Message
            );
        }
        catch (DomainException exception)
        {
            logger.Warning(exception, "Business rule refused hub command {CommandId}.", commandId);
            return DungeonCommandResult.Refuse(
                commandId,
                DungeonCommandErrorCodes.Refused,
                exception.Message
            );
        }
        catch (DbUpdateConcurrencyException exception)
        {
            logger.Warning(exception, "Concurrent update on hub command {CommandId}.", commandId);
            return DungeonCommandResult.Refuse(
                commandId,
                DungeonCommandErrorCodes.Conflict,
                "The run was changed by another command. Resynchronize and try again."
            );
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.Error(exception, "Unhandled exception on hub command {CommandId}.", commandId);
            return DungeonCommandResult.Refuse(
                commandId,
                DungeonCommandErrorCodes.Internal,
                "Internal server error."
            );
        }
    }

    private string? GetUserId()
    {
        string? userId = Context.GetHttpContext()?.Request.Headers[UserIdHeader];
        return string.IsNullOrWhiteSpace(userId) ? null : userId;
    }
}
