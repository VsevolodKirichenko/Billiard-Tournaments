using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Tournaments.Core.Domain;

namespace Tournaments.Api.Middleware;

public sealed class ApiExceptionHandler(ILogger<ApiExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken ct)
    {
        if (exception is OperationCanceledException && ct.IsCancellationRequested) return false;
        var status = exception switch
        {
            DomainException => 400, NotFoundException => 404, ConflictException => 409, UnauthorizedAccessException => 403, _ => 500
        };
        if (status == 500) logger.LogError(exception, "Unhandled API error");
        context.Response.StatusCode = status;
        await context.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = status, Title = status == 500 ? "Internal server error" : exception.Message,
            Extensions = { ["traceId"] = context.TraceIdentifier }
        }, ct);
        return true;
    }
}
