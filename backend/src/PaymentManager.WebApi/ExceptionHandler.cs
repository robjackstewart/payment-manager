using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using static PaymentManager.Application.Common.Exceptions;

namespace PaymentManager.WebApi;

internal sealed class ExceptionHandler : IExceptionHandler
{
    public ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
        => exception switch
        {
            ValidationException validationException => HandleValidationException(httpContext, validationException, cancellationToken),
            NotFoundException notFoundException => HandleNotFoundException(httpContext, notFoundException, cancellationToken),
            _ => HandleDefaultException(httpContext, exception, cancellationToken)
        };

    private static async ValueTask<bool> HandleNotFoundException(HttpContext httpContext, NotFoundException exception, CancellationToken cancellationToken)
    {
        httpContext.Response.StatusCode = StatusCodes.Status404NotFound;
        httpContext.Response.ContentType = "application/problem+json";
        var problemDetails = new ProblemDetails
        {
            Title = $"{exception.Type.Name} not found",
            Detail = exception.Message,
            Type = "https://httpstatuses.com/404"
        };
        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);
        return true;
    }

    private static async ValueTask<bool> HandleValidationException(HttpContext httpContext, ValidationException exception, CancellationToken cancellationToken)
    {
        httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
        httpContext.Response.ContentType = "application/problem+json";
        var problemDetails = new ValidationProblemDetails
        {
            Title = "Invalid request",
            Detail = "One or more validation errors occurred.",
            Errors = exception.Errors.ToDictionary(x => x.PropertyName, x => x.Errors.ToArray()),
            Type = "https://httpstatuses.com/400"
        };
        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);
        return true;
    }

    private static async ValueTask<bool> HandleDefaultException(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
        httpContext.Response.ContentType = "application/problem+json";
        var problemDetails = new ProblemDetails
        {
            Title = "An unexpected error occurred",
            Detail = exception.Message,
            Type = "https://httpstatuses.com/500"
        };
        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);
        return true;
    }
}
