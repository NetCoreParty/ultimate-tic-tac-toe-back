namespace UltimateTicTacToe.Core.Features.Rooms;

public class RoomsExpirySweeper
{
    private readonly IRoomStore _rooms;
    private readonly IMatchmakingTicketStore _tickets;
    private readonly IRoomsNotifier _notifier;

    public RoomsExpirySweeper(IRoomStore rooms, IMatchmakingTicketStore tickets, IRoomsNotifier notifier)
    {
        _rooms = rooms;
        _tickets = tickets;
        _notifier = notifier;
    }

    public async Task SweepOnceAsync(DateTime nowUtc, int batchSize, CancellationToken ct)
    {
        // Expired tickets (queued -> expired)
        var expiredTickets = await _tickets.GetExpiredQueuedTicketsAsync(nowUtc, take: batchSize, ct);
        foreach (var t in expiredTickets)
        {
            // Mark first (idempotent gate), then notify.
            var marked = await _tickets.TryMarkExpiredAsync(t.TicketId, ct);
            if (!marked) continue;

            await _notifier.NotifyQueueExpiredAsync(t.UserId, t.TicketId, ct);
        }

        // Expired half-full rooms (waiting with 1 player) -> notify + delete
        var expiredRooms = await _rooms.GetExpiredHalfFullWaitingRoomsAsync(nowUtc, take: batchSize, ct);
        foreach (var r in expiredRooms)
        {
            var userId = r.Players[0].UserId;
            await _notifier.NotifyRoomExpiredAsync(userId, r.RoomId, r.Type, ct);
            await _rooms.DeleteRoomAsync(r.RoomId, ct);
        }
    }
}

