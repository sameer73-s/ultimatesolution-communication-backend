using FluentValidation;
using UltimateSolution.Domain.Enums;

namespace UltimateSolution.Application.Features.Chat;

public sealed class CreateChannelCommandValidator : AbstractValidator<CreateChannelCommand>
{
    public CreateChannelCommandValidator()
    {
        RuleFor(command => command.RequestingUserId).NotEmpty();
        RuleFor(command => command.Type).IsInEnum();
        RuleFor(command => command.MemberIds).NotNull();
        RuleForEach(command => command.MemberIds).NotEmpty();
        When(command => command.Type is ChatChannelType.Group or ChatChannelType.Channel, () =>
            RuleFor(command => command.Name).NotEmpty().MaximumLength(120));
        When(command => command.Name is not null, () =>
            RuleFor(command => command.Name).MaximumLength(120));
    }
}

public sealed class AddChannelMemberCommandValidator : AbstractValidator<AddChannelMemberCommand>
{
    public AddChannelMemberCommandValidator()
    {
        RuleFor(command => command.RequestingUserId).NotEmpty();
        RuleFor(command => command.ChannelId).NotEmpty();
        RuleFor(command => command.MemberUserId).NotEmpty();
    }
}

public sealed class UpdateChannelCommandValidator : AbstractValidator<UpdateChannelCommand>
{
    public UpdateChannelCommandValidator()
    {
        RuleFor(command => command.RequestingUserId).NotEmpty();
        RuleFor(command => command.ChannelId).NotEmpty();
        RuleFor(command => command).Must(command => command.Name is not null || command.IsArchived.HasValue)
            .WithMessage("At least one channel update value is required.");
        When(command => command.Name is not null, () =>
            RuleFor(command => command.Name).NotEmpty().MaximumLength(120));
    }
}

public sealed class RemoveChannelMemberCommandValidator : AbstractValidator<RemoveChannelMemberCommand>
{
    public RemoveChannelMemberCommandValidator()
    {
        RuleFor(command => command.RequestingUserId).NotEmpty();
        RuleFor(command => command.ChannelId).NotEmpty();
        RuleFor(command => command.MemberUserId).NotEmpty();
    }
}

public sealed class SendChatMessageCommandValidator : AbstractValidator<SendChatMessageCommand>
{
    public SendChatMessageCommandValidator()
    {
        RuleFor(command => command.RequestingUserId).NotEmpty();
        RuleFor(command => command.ChannelId).NotEmpty();
        RuleFor(command => command.Body).NotEmpty().MaximumLength(4000);
    }
}

public sealed class UpdateChatMessageCommandValidator : AbstractValidator<UpdateChatMessageCommand>
{
    public UpdateChatMessageCommandValidator()
    {
        RuleFor(command => command.RequestingUserId).NotEmpty();
        RuleFor(command => command.MessageId).NotEmpty();
        RuleFor(command => command.Body).NotEmpty().MaximumLength(4000);
    }
}

public sealed class DeleteChatMessageCommandValidator : AbstractValidator<DeleteChatMessageCommand>
{
    public DeleteChatMessageCommandValidator()
    {
        RuleFor(command => command.RequestingUserId).NotEmpty();
        RuleFor(command => command.MessageId).NotEmpty();
    }
}

public sealed class GetChannelMessagesQueryValidator : AbstractValidator<GetChannelMessagesQuery>
{
    public GetChannelMessagesQueryValidator()
    {
        RuleFor(query => query.RequestingUserId).NotEmpty();
        RuleFor(query => query.ChannelId).NotEmpty();
        RuleFor(query => query.Take).InclusiveBetween(1, 100);
        When(query => query.SearchTerm is not null, () =>
            RuleFor(query => query.SearchTerm).MaximumLength(200));
    }
}

public sealed class MarkMessageReadCommandValidator : AbstractValidator<MarkMessageReadCommand>
{
    public MarkMessageReadCommandValidator()
    {
        RuleFor(command => command.RequestingUserId).NotEmpty();
        RuleFor(command => command.MessageId).NotEmpty();
    }
}

public sealed class VerifyChannelMembershipQueryValidator : AbstractValidator<VerifyChannelMembershipQuery>
{
    public VerifyChannelMembershipQueryValidator()
    {
        RuleFor(query => query.RequestingUserId).NotEmpty();
        RuleFor(query => query.ChannelId).NotEmpty();
    }
}

public sealed class ClientConnectedCommandValidator : AbstractValidator<ClientConnectedCommand>
{
    public ClientConnectedCommandValidator()
    {
        RuleFor(command => command.UserId).NotEmpty();
        RuleFor(command => command.ConnectionId).NotEmpty().MaximumLength(256);
    }
}

public sealed class ClientDisconnectedCommandValidator : AbstractValidator<ClientDisconnectedCommand>
{
    public ClientDisconnectedCommandValidator()
    {
        RuleFor(command => command.UserId).NotEmpty();
        RuleFor(command => command.ConnectionId).NotEmpty().MaximumLength(256);
    }
}

public sealed class SetPresenceStatusCommandValidator : AbstractValidator<SetPresenceStatusCommand>
{
    public SetPresenceStatusCommandValidator()
    {
        RuleFor(command => command.UserId).NotEmpty();
        RuleFor(command => command.Status).IsInEnum().NotEqual(PresenceStatus.Offline);
    }
}

public sealed class GetPresenceQueryValidator : AbstractValidator<GetPresenceQuery>
{
    public GetPresenceQueryValidator()
    {
        RuleFor(query => query.UserId).NotEmpty();
    }
}
