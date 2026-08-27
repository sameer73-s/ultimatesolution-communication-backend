using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using UltimateSolution.API.Common.Models;

namespace UltimateSolution.API.Common.Authorization;

public sealed class ApiAuthorizationMiddlewareResultHandler : IAuthorizationMiddlewareResultHandler
{
    private readonly AuthorizationMiddlewareResultHandler _defaultHandler = new();

    public async Task HandleAsync(
        RequestDelegate next,
        HttpContext context,
        AuthorizationPolicy policy,
        PolicyAuthorizationResult authorizeResult)
    {
        if (authorizeResult.Challenged)
        {
            await WriteFailureAsync(
                context,
                StatusCodes.Status401Unauthorized,
                "Authentication is required.",
                ["unauthorized"]);
            return;
        }

        if (authorizeResult.Forbidden)
        {
            await WriteFailureAsync(
                context,
                StatusCodes.Status403Forbidden,
                "You do not have permission to access this resource.",
                ["forbidden"]);
            return;
        }

        await _defaultHandler.HandleAsync(next, context, policy, authorizeResult);
    }

    private static Task WriteFailureAsync(
        HttpContext context,
        int statusCode,
        string message,
        IReadOnlyCollection<string> errors)
    {
        context.Response.StatusCode = statusCode;
        return context.Response.WriteAsJsonAsync(ApiResponse.Failure<object>(message, [.. errors]));
    }
}
