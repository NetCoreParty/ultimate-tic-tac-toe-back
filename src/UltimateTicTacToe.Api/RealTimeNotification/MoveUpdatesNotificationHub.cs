using Microsoft.AspNetCore.SignalR;
using UltimateTicTacToe.API.Hubs;
using UltimateTicTacToe.Core.Features.RealTimeNotification;
using UltimateTicTacToe.Core.Projections;

namespace UltimateTicTacToe.API.RealTimeNotification;

public class MoveUpdatesNotificationHub : IMoveUpdatesNotificationHub
{
    private readonly IHubContext<MoveUpdatesHub> _hubContext;

    public MoveUpdatesNotificationHub(IHubContext<MoveUpdatesHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public Task NotifyMoveAppliedAsync(string groupName, MoveResult moveResult, CancellationToken ct = default)
        => _hubContext.Clients.Group(groupName).SendAsync("MoveApplied", moveResult, ct);

    public Task NotifyMoveRejectedAsync(string groupName, string? errorDescription, CancellationToken ct = default)
        => _hubContext.Clients.Group(groupName).SendAsync("MoveRejected", errorDescription, ct);

    public Task NotifyGameEndedAsync(string groupName, GameEndedNotification payload, CancellationToken ct = default)
        => _hubContext.Clients.Group(groupName).SendAsync("GameEnded", payload, ct);

    public Task NotifyOpponentDisconnectedAsync(string groupName, OpponentPresenceNotification payload, CancellationToken ct = default)
        => _hubContext.Clients.Group(groupName).SendAsync("OpponentDisconnected", payload, ct);

    public Task NotifyOpponentReconnectedAsync(string groupName, OpponentPresenceNotification payload, CancellationToken ct = default)
        => _hubContext.Clients.Group(groupName).SendAsync("OpponentReconnected", payload, ct);
}