using BasketSystem.Application.Common;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace BasketSystem.Api.ErrorHandling;

public sealed class GlobalExceptionHandler(IProblemDetailsService problemDetailsService) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var (status, title, detail) = Map(exception);
        httpContext.Response.StatusCode = status;

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = new ProblemDetails
            {
                Status = status,
                Title = title,
                Detail = detail
            }
        });
    }

    private static (int Status, string Title, string? Detail) Map(Exception exception) => exception switch
    {
        ValidationException => (StatusCodes.Status400BadRequest, "Validation failed", exception.Message),
        NotFoundException => (StatusCodes.Status404NotFound, "Resource not found", exception.Message),
        HttpRequestException or TimeoutException => (
            StatusCodes.Status502BadGateway,
            "Upstream service unavailable",
            "The upstream Code Challenge API could not be reached."),
        _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred", null)
    };
}
