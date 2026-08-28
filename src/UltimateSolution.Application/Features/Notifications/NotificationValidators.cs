using FluentValidation;

namespace UltimateSolution.Application.Features.Notifications;

public sealed class GetNotificationsQueryValidator : AbstractValidator<GetNotificationsQuery>
{
    public GetNotificationsQueryValidator()
    {
        RuleFor(query => query.RequestingUserId).NotEmpty();
    }
}

public sealed class MarkNotificationReadCommandValidator : AbstractValidator<MarkNotificationReadCommand>
{
    public MarkNotificationReadCommandValidator()
    {
        RuleFor(command => command.RequestingUserId).NotEmpty();
        RuleFor(command => command.NotificationId).NotEmpty();
    }
}
