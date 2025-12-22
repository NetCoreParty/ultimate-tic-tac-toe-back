using Microsoft.AspNetCore.SignalR;
using UltimateTicTacToe.API.Hubs;
using UltimateTicTacToe.Core.Features.Rooms;

namespace UltimateTicTacToe.API.RealTimeNotification;

public class RoomsNotificationHub : IRoomsNotifier
{
    private readonly IHubContext<RoomsHub> _hubContext;

    public RoomsNotificationHub(IHubContext<RoomsHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public Task NotifyMatchFoundAsync(Guid userId, MatchFoundNotification payload, CancellationToken ct)
        => _hubContext.Clients.Group(userId.ToString()).SendAsync("MatchFound", payload, ct);

    public Task NotifyQueueJoinedAsync(Guid userId, QueueForGameResponse payload, CancellationToken ct)
        => _hubContext.Clients.Group(userId.ToString()).SendAsync("QueueJoined", payload, ct);

    public Task NotifyQueueExpiredAsync(Guid userId, Guid ticketId, CancellationToken ct)
        => _hubContext.Clients.Group(userId.ToString()).SendAsync("QueueExpired", new { TicketId = ticketId }, ct);

    public Task NotifyPrivateRoomCreatedAsync(Guid userId, CreatePrivateRoomResponse payload, CancellationToken ct)
        => _hubContext.Clients.Group(userId.ToString()).SendAsync("PrivateRoomCreated", payload, ct);

    public Task NotifyRoomExpiredAsync(Guid userId, Guid roomId, RoomType type, CancellationToken ct)
        => _hubContext.Clients.Group(userId.ToString()).SendAsync("RoomExpired", new { RoomId = roomId, Type = type }, ct);
}

