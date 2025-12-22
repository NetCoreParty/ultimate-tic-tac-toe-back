using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using UltimateTicTacToe.Core.Features.Rooms;

namespace UltimateTicTacToe.API.HostedServices;

/// <summary>
/// Best-effort sweeper to notify users about expired queue tickets / half-full waiting rooms.
/// Mongo TTL deletion won't emit application-level events, so we do it here.
/// </summary>
public class RoomsExpiryWorker : BackgroundService
{
    private readonly IRoomStore _rooms;
    private readonly IMatchmakingTicketStore _tickets;
    private readonly IRoomsNotifier _notifier;
    private readonly ILogger<RoomsExpiryWorker> _logger;

    public RoomsExpiryWorker(IRoomStore rooms, IMatchmakingTicketStore tickets, IRoomsNotifier notifier, ILogger<RoomsExpiryWorker> logger)
    {
        _rooms = rooms;
        _tickets = tickets;
        _notifier = notifier;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await SweepOnceAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // shutdown
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "RoomsExpiryWorker sweep failed.");
            }

            try
            {
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // shutdown
            }
        }
    }

    private async Task SweepOnceAsync(CancellationToken ct)
    {
        var now = DateTime.UtcNow;

        // Expired tickets (queued -> expired)
        var expiredTickets = await _tickets.GetExpiredQueuedTicketsAsync(now, take: 200, ct);
        foreach (var t in expiredTickets)
        {
            // Mark first (idempotent gate), then notify.
            var marked = await _tickets.TryMarkExpiredAsync(t.TicketId, ct);
            if (!marked) continue;

            await _notifier.NotifyQueueExpiredAsync(t.UserId, t.TicketId, ct);
        }

        // Expired half-full rooms (waiting with 1 player) -> notify + delete
        var expiredRooms = await _rooms.GetExpiredHalfFullWaitingRoomsAsync(now, take: 200, ct);
        foreach (var r in expiredRooms)
        {
            var userId = r.Players[0].UserId;
            await _notifier.NotifyRoomExpiredAsync(userId, r.RoomId, r.Type, ct);
            await _rooms.DeleteRoomAsync(r.RoomId, ct);
        }
    }
}

