using UltimateSolution.Application.Features.Chat;
using UltimateSolution.Domain.Enums;

namespace UltimateSolution.Application.Interfaces;

public interface IPresenceTracker
{
    PresenceSnapshot Connect(Guid userId, string connectionId, DateTimeOffset occurredAtUtc);

    PresenceSnapshot Disconnect(Guid userId, string connectionId, DateTimeOffset occurredAtUtc);

    PresenceSnapshot SetStatus(Guid userId, PresenceStatus status, DateTimeOffset occurredAtUtc);

    PresenceSnapshot GetStatus(Guid userId, DateTimeOffset observedAtUtc);
}
