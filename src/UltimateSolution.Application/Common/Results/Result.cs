namespace UltimateSolution.Application.Common.Results;

public record Result
{
    protected Result(bool isSuccess, string? errorCode)
    {
        IsSuccess = isSuccess;
        ErrorCode = errorCode;
    }

    public bool IsSuccess { get; }

    public string? ErrorCode { get; }

    public static Result Success() => new(true, null);

    public static Result Failure(string errorCode) => new(false, errorCode);
}

public sealed record Result<T> : Result
{
    internal Result(bool isSuccess, T? value, string? errorCode)
        : base(isSuccess, errorCode)
    {
        Value = value;
    }

    public T? Value { get; }

}

public static class ResultFactory
{
    public static Result<T> Success<T>(T value) => new(true, value, null);

    public static Result<T> Failure<T>(string errorCode) => new(false, default, errorCode);
}
