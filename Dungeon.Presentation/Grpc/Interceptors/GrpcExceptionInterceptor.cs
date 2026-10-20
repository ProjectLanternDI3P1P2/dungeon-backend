using Dungeon.Domain.Exceptions;
using FluentValidation;
using Grpc.Core;
using Grpc.Core.Interceptors;
using Microsoft.EntityFrameworkCore;
using ILogger = Serilog.ILogger;

namespace Dungeon.Presentation.Grpc.Interceptors;

/// <summary>
/// Maps application exceptions to standard gRPC statuses for every gRPC endpoint, as
/// <c>ExceptionHandlingMiddleware</c> does for HTTP.
/// </summary>
public sealed class GrpcExceptionInterceptor(ILogger logger, IHostEnvironment environment)
    : Interceptor
{
    public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>(
        TRequest request,
        ServerCallContext context,
        UnaryServerMethod<TRequest, TResponse> continuation
    )
    {
        try
        {
            return await continuation(request, context);
        }
        catch (RpcException)
        {
            throw;
        }
        catch (KeyNotFoundException exception)
        {
            logger.Warning(exception, "gRPC resource not found.");
            throw new RpcException(new Status(StatusCode.NotFound, exception.Message));
        }
        catch (ValidationException exception)
        {
            logger.Warning(exception, "gRPC validation error.");
            string detail = string.Join(" ", exception.Errors.Select(error => error.ErrorMessage));
            throw new RpcException(
                new Status(
                    StatusCode.InvalidArgument,
                    string.IsNullOrEmpty(detail) ? "Validation error." : detail
                )
            );
        }
        catch (DungeonRunAlreadyExistsException exception)
        {
            logger.Warning(exception, "gRPC resource already exists.");
            throw new RpcException(new Status(StatusCode.AlreadyExists, exception.Message));
        }
        catch (DomainException exception)
        {
            logger.Warning(exception, "Business rule refused the gRPC operation.");
            throw new RpcException(new Status(StatusCode.FailedPrecondition, exception.Message));
        }
        catch (DbUpdateConcurrencyException exception)
        {
            logger.Warning(exception, "Concurrent update detected during a gRPC call.");
            throw new RpcException(
                new Status(
                    StatusCode.Aborted,
                    "The resource was modified by another request. Retry the call."
                )
            );
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.Error(exception, "Unhandled gRPC exception.");
            string detail = environment.IsDevelopment()
                ? exception.Message
                : "Internal server error.";
            throw new RpcException(new Status(StatusCode.Internal, detail));
        }
    }
}
