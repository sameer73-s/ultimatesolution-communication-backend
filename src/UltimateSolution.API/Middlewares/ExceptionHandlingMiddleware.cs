using UltimateSolution.API.Common.Models;
using UltimateSolution.Domain.Exceptions;

namespace UltimateSolution.API.Middlewares;

public sealed class ExceptionHandlingMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        try
        {
            await next(context);
        }
        catch (DomainValidationException exception)
        {
            await WriteFailureAsync(context, StatusCodes.Status400BadRequest, exception.Message, exception.ErrorCode);
        }
        catch (DomainNotFoundException exception)
        {
            await WriteFailureAsync(context, StatusCodes.Status404NotFound, exception.Message, exception.ErrorCode);
        }
        catch (UnauthorizedAccessException)
        {
            await WriteFailureAsync(context, StatusCodes.Status401Unauthorized, "Authentication is required.", "unauthorized");
        }
        catch (Exception)
        {
            await WriteFailureAsync(
                context,
                StatusCodes.Status500InternalServerError,
                "An unexpected error occurred.",
                "internal_server_error");
        }
    }

    private static Task WriteFailureAsync(
        HttpContext context,
        int statusCode,
        string message,
        string error) 
    {
        context.Response.StatusCode = statusCode;
        return context.Response.WriteAsJsonAsync(ApiResponse.Failure<object>(message, error));
    }
}
