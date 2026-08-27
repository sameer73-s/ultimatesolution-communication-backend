using FluentValidation;

namespace UltimateSolution.Application.Features.AiSummary;

public sealed class RequestTranscriptionCommandValidator : AbstractValidator<RequestTranscriptionCommand>
{
    public RequestTranscriptionCommandValidator()
    {
        RuleFor(command => command.RequestingUserId).NotEmpty();
        RuleFor(command => command.RecordingId).NotEmpty();
    }
}

public sealed class GetMeetingTranscriptionQueryValidator : AbstractValidator<GetMeetingTranscriptionQuery>
{
    public GetMeetingTranscriptionQueryValidator()
    {
        RuleFor(query => query.RequestingUserId).NotEmpty();
        RuleFor(query => query.MeetingId).NotEmpty();
    }
}

public sealed class GenerateMeetingSummaryCommandValidator : AbstractValidator<GenerateMeetingSummaryCommand>
{
    public GenerateMeetingSummaryCommandValidator()
    {
        RuleFor(command => command.RequestingUserId).NotEmpty();
        RuleFor(command => command.MeetingId).NotEmpty();
    }
}

public sealed class GetMeetingSummaryQueryValidator : AbstractValidator<GetMeetingSummaryQuery>
{
    public GetMeetingSummaryQueryValidator()
    {
        RuleFor(query => query.RequestingUserId).NotEmpty();
        RuleFor(query => query.MeetingId).NotEmpty();
    }
}

public sealed class ApproveMeetingSummaryCommandValidator : AbstractValidator<ApproveMeetingSummaryCommand>
{
    public ApproveMeetingSummaryCommandValidator()
    {
        RuleFor(command => command.RequestingUserId).NotEmpty();
        RuleFor(command => command.MeetingId).NotEmpty();
    }
}

public sealed class GetActionItemsQueryValidator : AbstractValidator<GetActionItemsQuery>
{
    public GetActionItemsQueryValidator()
    {
        RuleFor(query => query.RequestingUserId).NotEmpty();
    }
}

public sealed class UpdateActionItemCommandValidator : AbstractValidator<UpdateActionItemCommand>
{
    public UpdateActionItemCommandValidator()
    {
        RuleFor(command => command.RequestingUserId).NotEmpty();
        RuleFor(command => command.ActionItemId).NotEmpty();
        RuleFor(command => command.Title).NotEmpty().MaximumLength(400);
        RuleFor(command => command.Description).MaximumLength(4000).When(command => command.Description is not null);
    }
}
