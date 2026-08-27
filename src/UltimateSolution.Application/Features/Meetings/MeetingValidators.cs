using FluentValidation;

namespace UltimateSolution.Application.Features.Meetings;

public sealed class ScheduleMeetingCommandValidator : AbstractValidator<ScheduleMeetingCommand>
{
    public ScheduleMeetingCommandValidator()
    {
        RuleFor(command => command.RequestingUserId).NotEmpty();
        RuleFor(command => command.Title).NotEmpty().MaximumLength(180);
        RuleFor(command => command.Agenda).MaximumLength(4000).When(command => command.Agenda is not null);
        RuleFor(command => command.ScheduledEndUtc).GreaterThan(command => command.ScheduledStartUtc);
        RuleFor(command => command.ParticipantUserIds).NotNull();
        RuleForEach(command => command.ParticipantUserIds).NotEmpty();
    }
}

public sealed class UpdateMeetingCommandValidator : AbstractValidator<UpdateMeetingCommand>
{
    public UpdateMeetingCommandValidator()
    {
        RuleFor(command => command.RequestingUserId).NotEmpty();
        RuleFor(command => command.MeetingId).NotEmpty();
        RuleFor(command => command.Title).NotEmpty().MaximumLength(180);
        RuleFor(command => command.Agenda).MaximumLength(4000).When(command => command.Agenda is not null);
        RuleFor(command => command.ScheduledEndUtc).GreaterThan(command => command.ScheduledStartUtc);
    }
}

public abstract class MeetingIdentifierValidator<T> : AbstractValidator<T> where T : class
{
    protected static IRuleBuilderOptions<T, Guid> RequiredId(IRuleBuilder<T, Guid> ruleBuilder) => ruleBuilder.NotEmpty();
}

public sealed class InviteMeetingParticipantCommandValidator : MeetingIdentifierValidator<InviteMeetingParticipantCommand>
{
    public InviteMeetingParticipantCommandValidator()
    {
        RequiredId(RuleFor(command => command.RequestingUserId));
        RequiredId(RuleFor(command => command.MeetingId));
        RequiredId(RuleFor(command => command.ParticipantUserId));
    }
}

public sealed class RemoveMeetingParticipantCommandValidator : MeetingIdentifierValidator<RemoveMeetingParticipantCommand>
{
    public RemoveMeetingParticipantCommandValidator()
    {
        RequiredId(RuleFor(command => command.RequestingUserId));
        RequiredId(RuleFor(command => command.MeetingId));
        RequiredId(RuleFor(command => command.ParticipantUserId));
    }
}
