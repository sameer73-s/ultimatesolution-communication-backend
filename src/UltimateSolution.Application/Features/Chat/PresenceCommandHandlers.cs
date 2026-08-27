using Mediator;
using UltimateSolution.Application.Interfaces;

namespace UltimateSolution.Application.Features.Chat;

public sealed class ClientConnectedCommandHandler(
    IPresenceTracker presenceTracker,
    IChatRealtimePublisher realtimePublisher)
    : IRequestHandler<ClientConnectedCommand, PresenceSnapshot>
{
    public async ValueTask<PresenceSnapshot> Handle(ClientConnectedCommand request, CancellationToken cancellationToken)
    {
        var presence = presenceTracker.Connect(request.UserId, request.ConnectionId, DateTimeOffset.UtcNow);
        await realtimePublisher.PublishPresenceChangedAsync(presence, cancellationToken);
        return presence;
    }
}

public sealed class ClientDisconnectedCommandHandler(
    IPresenceTracker presenceTracker,
    IChatRealtimePublisher realtimePublisher)
    : IRequestHandler<ClientDisconnectedCommand, PresenceSnapshot>
{
    public async ValueTask<PresenceSnapshot> Handle(ClientDisconnectedCommand request, CancellationToken cancellationToken)
    {
        var presence = presenceTracker.Disconnect(request.UserId, request.ConnectionId, DateTimeOffset.UtcNow);
        await realtimePublisher.PublishPresenceChangedAsync(presence, cancellationToken);
        return presence;
    }
}

public sealed class SetPresenceStatusCommandHandler(
    IPresenceTracker presenceTracker,
    IChatRealtimePublisher realtimePublisher)
    : IRequestHandler<SetPresenceStatusCommand, PresenceSnapshot>
{
    public async ValueTask<PresenceSnapshot> Handle(SetPresenceStatusCommand request, CancellationToken cancellationToken)
    {
        var presence = presenceTracker.SetStatus(request.UserId, request.Status, DateTimeOffset.UtcNow);
        await realtimePublisher.PublishPresenceChangedAsync(presence, cancellationToken);
        return presence;
    }
}

public sealed class GetPresenceQueryHandler(IPresenceTracker presenceTracker)
    : IRequestHandler<GetPresenceQuery, PresenceSnapshot>
{
    public ValueTask<PresenceSnapshot> Handle(GetPresenceQuery request, CancellationToken cancellationToken) =>
        ValueTask.FromResult(presenceTracker.GetStatus(request.UserId, DateTimeOffset.UtcNow));
}
