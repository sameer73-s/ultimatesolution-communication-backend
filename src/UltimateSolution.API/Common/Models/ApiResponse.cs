namespace UltimateSolution.API.Common.Models;

public sealed record ApiResponse<T>(
    bool Success,
    T? Data,
    string Message,
    IReadOnlyCollection<string> Errors);

public static class ApiResponse
{
    public static ApiResponse<T> Ok<T>(T data, string message = "Request completed successfully.") =>
        new(true, data, message, Array.Empty<string>());

    public static ApiResponse<T> Failure<T>(string message, params string[] errors) =>
        new(false, default, message, errors);
}
