using UltimateSolution.Domain.Enums;

namespace UltimateSolution.API.Contracts.Chat;

public sealed record CreateChannelRequest(
    ChatChannelType Type,
    string? Name,
    IReadOnlyCollection<Guid> MemberIds);

public sealed record AddChannelMemberRequest(Guid UserId);

public sealed record UpdateChannelRequest(string? Name, bool? IsArchived);

public sealed record SendChatMessageRequest(string Body);

public sealed record UpdateChatMessageRequest(string Body);
