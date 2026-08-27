namespace UltimateSolution.Domain.Exceptions;

public abstract class DomainException : Exception
{
    protected DomainException(string message, string errorCode)
        : base(message)
    {
        ErrorCode = errorCode;
    }

    public string ErrorCode { get; }
}
