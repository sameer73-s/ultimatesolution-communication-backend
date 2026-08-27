using Mediator;
using UltimateSolution.Domain.Enums;

namespace UltimateSolution.Application.Features.Chat;

public sealed record ChannelMemberDto(Guid UserId, ChannelMemberRole Role, DateTimeOffset JoinedAtUtc);

public sealed record ChannelDto(
    Guid Id,
    string Name,
    ChatChannelType Type,
    Guid CreatedByUserId,
    DateTimeOffset CreatedAtUtc,
    bool IsArchived,
    DateTimeOffset? ArchivedAtUtc,
    IReadOnlyCollection<ChannelMemberDto> Members);

public sealed record ChatMessageDto(
    Guid Id,
    Guid ChannelId,
    Guid SenderUserId,
    string Body,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? EditedAtUtc,
    DateTimeOffset? DeletedAtUtc);

public sealed record MessageReadDto(
    Guid ChannelId,
    Guid UserId,
    Guid LastReadMessageId,
    DateTimeOffset LastReadAtUtc);

public sealed record PresenceSnapshot(Guid UserId, PresenceStatus Status, DateTimeOffset ChangedAtUtc);

public sealed record CreateChannelCommand(
    Guid RequestingUserId,
    ChatChannelType Type,
    string? Name,
    IReadOnlyCollection<Guid> MemberIds) : IRequest<ChannelDto>;

public sealed record GetChannelsQuery(Guid RequestingUserId) : IRequest<IReadOnlyCollection<ChannelDto>>;

public sealed record GetChannelQuery(Guid RequestingUserId, Guid ChannelId) : IRequest<ChannelDto>;

public sealed record AddChannelMemberCommand(
    Guid RequestingUserId,
    bool IsAdministrator,
    Guid ChannelId,
    Guid MemberUserId) : IRequest<ChannelDto>;

public sealed record UpdateChannelCommand(
    Guid RequestingUserId,
    bool IsAdministrator,
    Guid ChannelId,
    string? Name,
    bool? IsArchived) : IRequest<ChannelDto>;

public sealed record RemoveChannelMemberCommand(
    Guid RequestingUserId,
    bool IsAdministrator,
    Guid ChannelId,
    Guid MemberUserId) : IRequest<ChannelDto>;

public sealed record SendChatMessageCommand(Guid RequestingUserId, Guid ChannelId, string Body) : IRequest<ChatMessageDto>;

public sealed record UpdateChatMessageCommand(
    Guid RequestingUserId,
    Guid MessageId,
    string Body) : IRequest<ChatMessageDto>;

public sealed record DeleteChatMessageCommand(
    Guid RequestingUserId,
    bool IsAdministrator,
    Guid MessageId) : IRequest<ChatMessageDto>;

public sealed record GetChannelMessagesQuery(
    Guid RequestingUserId,
    Guid ChannelId,
    string? SearchTerm,
    int Take) : IRequest<IReadOnlyCollection<ChatMessageDto>>;

public sealed record MarkMessageReadCommand(
    Guid RequestingUserId,
    Guid MessageId) : IRequest<MessageReadDto>;

public sealed record VerifyChannelMembershipQuery(Guid RequestingUserId, Guid ChannelId) : IRequest<Unit>;

public sealed record ClientConnectedCommand(Guid UserId, string ConnectionId) : IRequest<PresenceSnapshot>;

public sealed record ClientDisconnectedCommand(Guid UserId, string ConnectionId) : IRequest<PresenceSnapshot>;

public sealed record SetPresenceStatusCommand(Guid UserId, PresenceStatus Status) : IRequest<PresenceSnapshot>;

public sealed record GetPresenceQuery(Guid UserId) : IRequest<PresenceSnapshot>;
