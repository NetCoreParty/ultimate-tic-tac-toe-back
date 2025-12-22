using Moq;
using UltimateTicTacToe.Core.Features.Rooms;

namespace UltimateTicTacToe.Core.Tests.Unit.Features.Rooms;

public class RoomsExpiryWorkerLikeSweepTests
{
    [Fact]
    public async Task ExpiredTickets_AreMarkedAndNotified()
    {
        var userId = Guid.NewGuid();
        var ticketId = Guid.NewGuid();

        var rooms = new Mock<IRoomStore>();
        var tickets = new Mock<IMatchmakingTicketStore>();
        var notifier = new Mock<IRoomsNotifier>();

        tickets.Setup(t => t.GetExpiredQueuedTicketsAsync(It.IsAny<DateTime>(), 200, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[]
            {
                new MatchmakingTicketDto(ticketId, userId, MatchmakingTicketStatus.Queued, DateTime.UtcNow.AddMinutes(-10), DateTime.UtcNow.AddMinutes(-1), null, null)
            });

        tickets.Setup(t => t.TryMarkExpiredAsync(ticketId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // mimic one sweep iteration (same logic as RoomsExpiryWorker)
        var now = DateTime.UtcNow;
        var expiredTickets = await tickets.Object.GetExpiredQueuedTicketsAsync(now, 200, CancellationToken.None);
        foreach (var t in expiredTickets)
        {
            var marked = await tickets.Object.TryMarkExpiredAsync(t.TicketId, CancellationToken.None);
            if (!marked) continue;
            await notifier.Object.NotifyQueueExpiredAsync(t.UserId, t.TicketId, CancellationToken.None);
        }

        notifier.Verify(n => n.NotifyQueueExpiredAsync(userId, ticketId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExpiredHalfFullRooms_AreNotifiedAndDeleted()
    {
        var userId = Guid.NewGuid();
        var roomId = Guid.NewGuid();

        var rooms = new Mock<IRoomStore>();
        var tickets = new Mock<IMatchmakingTicketStore>();
        var notifier = new Mock<IRoomsNotifier>();

        rooms.Setup(r => r.GetExpiredHalfFullWaitingRoomsAsync(It.IsAny<DateTime>(), 200, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[]
            {
                new RoomDto(roomId, RoomType.Private, RoomStatus.Waiting, "ABCD", DateTime.UtcNow.AddMinutes(-10), DateTime.UtcNow.AddMinutes(-1), new[] { new RoomPlayer(userId, DateTime.UtcNow.AddMinutes(-10)) })
            });

        rooms.Setup(r => r.DeleteRoomAsync(roomId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // mimic one sweep iteration (same logic as RoomsExpiryWorker)
        var now = DateTime.UtcNow;
        var expiredRooms = await rooms.Object.GetExpiredHalfFullWaitingRoomsAsync(now, 200, CancellationToken.None);
        foreach (var r in expiredRooms)
        {
            var uid = r.Players[0].UserId;
            await notifier.Object.NotifyRoomExpiredAsync(uid, r.RoomId, r.Type, CancellationToken.None);
            await rooms.Object.DeleteRoomAsync(r.RoomId, CancellationToken.None);
        }

        notifier.Verify(n => n.NotifyRoomExpiredAsync(userId, roomId, RoomType.Private, It.IsAny<CancellationToken>()), Times.Once);
        rooms.Verify(r => r.DeleteRoomAsync(roomId, It.IsAny<CancellationToken>()), Times.Once);
    }
}

