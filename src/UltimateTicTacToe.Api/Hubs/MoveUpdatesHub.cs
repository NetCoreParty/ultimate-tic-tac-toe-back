using Microsoft.AspNetCore.SignalR;

namespace UltimateTicTacToe.API.Hubs;

/// <summary>
/// Contains only methods that should be called from a client (Vue3 in our case)
/// </summary>
public class MoveUpdatesHub : Hub
{
    /// <summary>
    /// Join a per-game group so the client receives MoveApplied/MoveRejected notifications.
    /// Group name convention: gameId string.
    /// </summary>
    public Task JoinGame(string gameId)
        => Groups.AddToGroupAsync(Context.ConnectionId, gameId);

    /// <summary>
    /// Leave a per-game group.
    /// </summary>
    public Task LeaveGame(string gameId)
        => Groups.RemoveFromGroupAsync(Context.ConnectionId, gameId);

    // Maybe: notify others when a player connects or disconnects
    public override async Task OnConnectedAsync()
    {
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await base.OnDisconnectedAsync(exception);
    }
}