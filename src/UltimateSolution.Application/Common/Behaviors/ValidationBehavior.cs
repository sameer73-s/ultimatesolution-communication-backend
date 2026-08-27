using FluentValidation;
using MediatR;
using UltimateSolution.Application.Common.Exceptions;

namespace UltimateSolution.Application.Common.Behaviors;

public sealed class ValidationBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var validationContext = new ValidationContext<TRequest>(request);
        var validationResults = await Task.WhenAll(
            validators.Select(validator => validator.ValidateAsync(validationContext, cancellationToken)));
        var errors = validationResults
            .SelectMany(result => result.Errors)
            .Where(failure => failure is not null)
            .Select(failure => failure.ErrorMessage)
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        if (errors.Length > 0)
        {
            throw new ApplicationValidationException(errors);
        }

        return await next(cancellationToken);
    }
}
