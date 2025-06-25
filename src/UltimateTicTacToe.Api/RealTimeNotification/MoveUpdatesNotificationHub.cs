using Microsoft.AspNetCore.SignalR;
using UltimateTicTacToe.API.Hubs;
using UltimateTicTacToe.Core.Features.RealTimeNotification;

namespace UltimateTicTacToe.API.RealTimeNotification;

public class MoveUpdatesNotificationHub : IMoveUpdatesNotificationHub
{
    private readonly IHubContext<MoveUpdatesHub> _hubContext;

    public MoveUpdatesNotificationHub(IHubContext<MoveUpdatesHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public Task NotifyMoveAppliedAsync(string groupName, bool isMoveApplied, CancellationToken ct = default)
        => _hubContext.Clients.Group(groupName).SendAsync("MoveApplied", isMoveApplied, ct);

    public Task NotifyMoveRejectedAsync(string groupName, string? errorDescription, CancellationToken ct = default)
        => _hubContext.Clients.Group(groupName).SendAsync("MoveRejected", errorDescription, ct);
}