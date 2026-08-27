namespace UltimateSolution.Domain.Exceptions;

public sealed class DomainNotFoundException : DomainException
{
    public DomainNotFoundException(string message)
        : base(message, "not_found")
    {
    }
}
