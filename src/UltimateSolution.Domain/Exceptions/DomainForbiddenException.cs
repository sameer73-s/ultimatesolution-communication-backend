namespace UltimateSolution.Domain.Exceptions;

public sealed class DomainForbiddenException : DomainException
{
    public DomainForbiddenException(string message)
        : base(message, "forbidden")
    {
    }
}
