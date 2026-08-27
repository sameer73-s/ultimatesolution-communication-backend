using System.Collections.Concurrent;
using UltimateSolution.Application.Features.Chat;
using UltimateSolution.Application.Interfaces;
using UltimateSolution.Domain.Enums;

namespace UltimateSolution.Infrastructure.Presence;

public sealed class InMemoryPresenceTracker : IPresenceTracker
{
    private readonly ConcurrentDictionary<Guid, PresenceRecord> _presenceByUserId = new();

    public PresenceSnapshot Connect(Guid userId, string connectionId, DateTimeOffset occurredAtUtc)
    {
        var record = _presenceByUserId.GetOrAdd(userId, static _ => new PresenceRecord());
        record.ConnectionIds[connectionId] = 0;
        record.Status = PresenceStatus.Online;
        record.ChangedAtUtc = occurredAtUtc;
        return new PresenceSnapshot(userId, record.Status, record.ChangedAtUtc);
    }

    public PresenceSnapshot Disconnect(Guid userId, string connectionId, DateTimeOffset occurredAtUtc)
    {
        var record = _presenceByUserId.GetOrAdd(userId, static _ => new PresenceRecord());
        record.ConnectionIds.TryRemove(connectionId, out _);
        if (record.ConnectionIds.IsEmpty)
        {
            record.Status = PresenceStatus.Offline;
            record.ChangedAtUtc = occurredAtUtc;
        }

        return new PresenceSnapshot(userId, record.Status, record.ChangedAtUtc);
    }

    public PresenceSnapshot SetStatus(Guid userId, PresenceStatus status, DateTimeOffset occurredAtUtc)
    {
        var record = _presenceByUserId.GetOrAdd(userId, static _ => new PresenceRecord());
        record.Status = status;
        record.ChangedAtUtc = occurredAtUtc;
        return new PresenceSnapshot(userId, record.Status, record.ChangedAtUtc);
    }

    public PresenceSnapshot GetStatus(Guid userId, DateTimeOffset observedAtUtc)
    {
        if (_presenceByUserId.TryGetValue(userId, out var record))
        {
            return new PresenceSnapshot(userId, record.Status, record.ChangedAtUtc);
        }

        return new PresenceSnapshot(userId, PresenceStatus.Offline, observedAtUtc);
    }

    private sealed class PresenceRecord
    {
        public ConcurrentDictionary<string, byte> ConnectionIds { get; } = new();

        public PresenceStatus Status { get; set; } = PresenceStatus.Offline;

        public DateTimeOffset ChangedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    }
}
