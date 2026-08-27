namespace UltimateSolution.Application.Common.Exceptions;

public sealed class ApplicationValidationException(IReadOnlyCollection<string> errors)
    : Exception("One or more validation errors occurred.")
{
    public IReadOnlyCollection<string> Errors { get; } = errors;
}
