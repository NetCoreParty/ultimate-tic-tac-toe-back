using Microsoft.AspNetCore.SignalR;
using UltimateTicTacToe.Core.Features.RealTimeNotification;
using UltimateTicTacToe.Core.Projections;

namespace UltimateTicTacToe.API.Hubs;

/// <summary>
/// Contains only methods that should be called from a client (Vue3 in our case)
/// </summary>
public class MoveUpdatesHub : Hub
{
    private readonly IMoveUpdatesNotificationHub _notifier;

    public MoveUpdatesHub(IMoveUpdatesNotificationHub notifier)
    {
        _notifier = notifier;
    }

    /// <summary>
    /// Join a per-game group so the client receives MoveApplied/MoveRejected notifications.
    /// Group name convention: gameId string.
    /// Soft-leave rejoin also broadcasts OpponentReconnected when X-User-Id is present.
    /// </summary>
    public async Task JoinGame(string gameId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, gameId);

        if (TryGetUserId(out var userId) && Guid.TryParse(gameId, out var parsedGameId))
        {
            await _notifier.NotifyOpponentReconnectedAsync(
                gameId,
                new OpponentPresenceNotification(parsedGameId, userId));
        }
    }

    /// <summary>
    /// Leave a per-game group.
    /// </summary>
    public Task LeaveGame(string gameId)
        => Groups.RemoveFromGroupAsync(Context.ConnectionId, gameId);

    public override async Task OnConnectedAsync()
    {
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await base.OnDisconnectedAsync(exception);
    }

    private bool TryGetUserId(out Guid userId)
    {
        userId = Guid.Empty;
        var http = Context.GetHttpContext();
        if (http is null)
            return false;
        if (!http.Request.Headers.TryGetValue("X-User-Id", out var values))
            return false;
        return Guid.TryParse(values.FirstOrDefault(), out userId);
    }
}
