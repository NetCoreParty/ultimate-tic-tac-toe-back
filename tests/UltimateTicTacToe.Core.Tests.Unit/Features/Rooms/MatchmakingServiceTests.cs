using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using UltimateTicTacToe.Core.Configuration;
using UltimateTicTacToe.Core.Features.Rooms;
using UltimateTicTacToe.Core.Projections;
using UltimateTicTacToe.Core.Services;

namespace UltimateTicTacToe.Core.Tests.Unit.Features.Rooms;

public class MatchmakingServiceTests
{
    [Fact]
    public async Task CreatePrivateRoom_ShouldFail_WhenAtPrivateRoomCap()
    {
        var rooms = new Mock<IRoomStore>();
        rooms.Setup(r => r.CountActiveRoomsAsync(RoomType.Private, It.IsAny<CancellationToken>()))
            .ReturnsAsync(50);

        var tickets = new Mock<IMatchmakingTicketStore>();
        var metrics = new Mock<IRoomMetricsStore>();
        var games = new Mock<IGameRepository>();
        games.SetupGet(g => g.GamesNow).Returns(0);
        var notifier = new Mock<IRoomsNotifier>();
        var logger = new Mock<ILogger<MatchmakingService>>();

        var svc = new MatchmakingService(
            rooms.Object,
            tickets.Object,
            metrics.Object,
            games.Object,
            notifier.Object,
            Options.Create(new RoomSettings { RoomTtlMinutes = 5, MaxRegularRooms = 75, MaxPrivateRooms = 50 }),
            Options.Create(new GameplaySettings { MaxActiveGames = 140, BackpressureThresholdPercent = 90 }),
            logger.Object
        );

        var result = await svc.CreatePrivateRoomAsync(Guid.NewGuid(), CancellationToken.None);
        Assert.False(result.IsSuccess);
        Assert.Equal(429, result.Code);
    }

    [Fact]
    public async Task QueueAsync_ShouldCreateWaitingRoom_WhenNoMatchAvailable()
    {
        var userId = Guid.NewGuid();
        var rooms = new Mock<IRoomStore>();
        rooms.Setup(r => r.CountActiveRoomsAsync(RoomType.Regular, It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);
        rooms.Setup(r => r.TryJoinWaitingRegularRoomAsync(userId, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((RoomDto?)null);

        rooms.Setup(r => r.CreateWaitingRegularRoomAsync(userId, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RoomDto(Guid.NewGuid(), RoomType.Regular, RoomStatus.Waiting, null, DateTime.UtcNow, DateTime.UtcNow.AddMinutes(5), new[] { new RoomPlayer(userId, DateTime.UtcNow) }));

        var tickets = new Mock<IMatchmakingTicketStore>();
        tickets.Setup(t => t.CreateQueuedTicketAsync(userId, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MatchmakingTicketDto(Guid.NewGuid(), userId, MatchmakingTicketStatus.Queued, DateTime.UtcNow, DateTime.UtcNow.AddMinutes(5), null, null));

        var metrics = new Mock<IRoomMetricsStore>();
        var games = new Mock<IGameRepository>();
        games.SetupGet(g => g.GamesNow).Returns(0);
        var notifier = new Mock<IRoomsNotifier>();
        var logger = new Mock<ILogger<MatchmakingService>>();

        var svc = new MatchmakingService(
            rooms.Object,
            tickets.Object,
            metrics.Object,
            games.Object,
            notifier.Object,
            Options.Create(new RoomSettings { RoomTtlMinutes = 5, MaxRegularRooms = 75, MaxPrivateRooms = 50 }),
            Options.Create(new GameplaySettings { MaxActiveGames = 140, BackpressureThresholdPercent = 90 }),
            logger.Object
        );

        var result = await svc.QueueAsync(userId, CancellationToken.None);
        Assert.True(result.IsSuccess);

        rooms.Verify(r => r.CreateWaitingRegularRoomAsync(userId, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Once);
        metrics.Verify(m => m.IncrementRoomsCreatedAsync(RoomType.Regular, It.IsAny<CancellationToken>()), Times.Once);
        notifier.Verify(n => n.NotifyQueueJoinedAsync(userId, It.IsAny<QueueForGameResponse>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}

