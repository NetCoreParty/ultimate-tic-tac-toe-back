using Microsoft.AspNetCore.SignalR;

namespace UltimateTicTacToe.API.Hubs;

/// <summary>
/// Matchmaking/rooms notifications hub. Clients join a per-user group using X-User-Id header.
/// </summary>
public class RoomsHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        var userId = TryGetUserId();
        if (userId != null)
            await Groups.AddToGroupAsync(Context.ConnectionId, userId.Value.ToString());

        await base.OnConnectedAsync();
    }

    public Task JoinUser()
    {
        var userId = TryGetUserId();
        if (userId == null)
            throw new HubException("Missing or invalid X-User-Id header.");

        return Groups.AddToGroupAsync(Context.ConnectionId, userId.Value.ToString());
    }

    private Guid? TryGetUserId()
    {
        var http = Context.GetHttpContext();
        if (http == null) return null;

        if (!http.Request.Headers.TryGetValue("X-User-Id", out var values))
            return null;

        return Guid.TryParse(values.FirstOrDefault(), out var id) ? id : null;
    }
}

