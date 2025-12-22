using Moq;
using UltimateTicTacToe.Core.Features.Rooms;

namespace UltimateTicTacToe.Core.Tests.Unit.Features.Rooms;

public class RoomsExpirySweeperTests
{
    [Fact]
    public async Task SweepOnceAsync_MarksExpiredTickets_AndNotifies()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var userId = Guid.NewGuid();
        var ticketId = Guid.NewGuid();

        var rooms = new Mock<IRoomStore>();
        var tickets = new Mock<IMatchmakingTicketStore>();
        var notifier = new Mock<IRoomsNotifier>();

        tickets.Setup(t => t.GetExpiredQueuedTicketsAsync(now, 200, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[]
            {
                new MatchmakingTicketDto(ticketId, userId, MatchmakingTicketStatus.Queued, now.AddMinutes(-10), now.AddMinutes(-1), null, null)
            });

        tickets.Setup(t => t.TryMarkExpiredAsync(ticketId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        rooms.Setup(r => r.GetExpiredHalfFullWaitingRoomsAsync(now, 200, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<RoomDto>());

        var sut = new RoomsExpirySweeper(rooms.Object, tickets.Object, notifier.Object);

        // Act
        await sut.SweepOnceAsync(now, batchSize: 200, CancellationToken.None);

        // Assert
        notifier.Verify(n => n.NotifyQueueExpiredAsync(userId, ticketId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SweepOnceAsync_NotifiesRoomExpired_AndDeletesRoom()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var userId = Guid.NewGuid();
        var roomId = Guid.NewGuid();

        var rooms = new Mock<IRoomStore>();
        var tickets = new Mock<IMatchmakingTicketStore>();
        var notifier = new Mock<IRoomsNotifier>();

        tickets.Setup(t => t.GetExpiredQueuedTicketsAsync(now, 200, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<MatchmakingTicketDto>());

        rooms.Setup(r => r.GetExpiredHalfFullWaitingRoomsAsync(now, 200, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[]
            {
                new RoomDto(roomId, RoomType.Private, RoomStatus.Waiting, "ABCD", now.AddMinutes(-10), now.AddMinutes(-1), new[]
                {
                    new RoomPlayer(userId, now.AddMinutes(-10))
                })
            });

        rooms.Setup(r => r.DeleteRoomAsync(roomId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var sut = new RoomsExpirySweeper(rooms.Object, tickets.Object, notifier.Object);

        // Act
        await sut.SweepOnceAsync(now, batchSize: 200, CancellationToken.None);

        // Assert
        notifier.Verify(n => n.NotifyRoomExpiredAsync(userId, roomId, RoomType.Private, It.IsAny<CancellationToken>()), Times.Once);
        rooms.Verify(r => r.DeleteRoomAsync(roomId, It.IsAny<CancellationToken>()), Times.Once);
    }
}

