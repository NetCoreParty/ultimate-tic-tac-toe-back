using Microsoft.AspNetCore.SignalR;

namespace UltimateTicTacToe.API.Hubs;

/// <summary>
/// Contains only methods that should be called from a client (Vue3 in our case)
/// </summary>
public class MoveUpdatesHub : Hub
{
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