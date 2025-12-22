using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System.Collections.Concurrent;
using UltimateTicTacToe.Core.Configuration;
using UltimateTicTacToe.Core.Domain.Aggregate;
using UltimateTicTacToe.Core.Domain.Events;
using UltimateTicTacToe.Core.Features.GameSaving;
using UltimateTicTacToe.Core.Projections;
using UltimateTicTacToe.Core.Services;

namespace UltimateTicTacToe.Core.Tests.Unit.Features.Gameplay;

public class InMemoryGameRepositoryTests
{
    private readonly Mock<IEventStore> _eventStoreMock = new();
    private readonly Mock<ILogger<InMemoryGameRepository>> _loggerMock = new();
    private readonly IOptions<GameplaySettings> _settings;

    public InMemoryGameRepositoryTests()
    {
        _settings = Options.Create(new GameplaySettings
        {
            MaxActiveGames = 3,
            EventsUntilSnapshot = 5,
            BackpressureThresholdPercent = 90
        });
    }

    private InMemoryGameRepository CreateRepository()
    {
        return new InMemoryGameRepository(
            _eventStoreMock.Object,
            new StateSnapshotStore(_settings),
            _loggerMock.Object,
            _settings
        );
    }

    [Fact]
    public async Task TryStartGameAsync_ShouldSucceed_IfUnderLimit()
    {
        var repo = CreateRepository();

        var result = await repo.TryStartGameAsync();

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(1, repo.GamesNow);

        // Persisted + cleared uncommitted events
        _eventStoreMock.Verify(s => s.AppendEventsAsync(result.Value.GameId, It.IsAny<IEnumerable<IDomainEvent>>(), It.IsAny<CancellationToken>()), Times.Once);
        var game = GetPrivateGame(repo, result.Value.GameId);
        Assert.Equal(0, game.UncommittedChanges.Count);
    }

    [Fact]
    public async Task TryStartGameAsync_ShouldFail_WhenOverLimit()
    {
        var repo = CreateRepository();

        for (int i = 0; i < 3; i++)
        {
            var r = await repo.TryStartGameAsync();
            Assert.True(r.IsSuccess);
        }

        var result = await repo.TryStartGameAsync();

        Assert.False(result.IsSuccess);
        Assert.Equal(429, result.Code);
    }

    [Fact]
    public async Task TryStartGameAsync_ShouldFail_WhenInBackpressure()
    {
        // MaxActiveGames=3 and BackpressureThresholdPercent=90 -> threshold=ceil(2.7)=3
        // So with 2 active games: allowed; with 3 active games: cap hit (429); but threshold equals cap here.
        // Use a different setting to get threshold < cap.
        var customSettings = Options.Create(new GameplaySettings
        {
            MaxActiveGames = 10,
            EventsUntilSnapshot = 5,
            BackpressureThresholdPercent = 90 // threshold=9
        });

        var repo = new InMemoryGameRepository(_eventStoreMock.Object, new StateSnapshotStore(customSettings), _loggerMock.Object, customSettings);

        for (int i = 0; i < 9; i++)
        {
            var r = await repo.TryStartGameAsync();
            Assert.True(r.IsSuccess);
        }

        // 10th start should be rejected by backpressure (active=9, threshold=9)
        var result = await repo.TryStartGameAsync();
        Assert.False(result.IsSuccess);
        Assert.Equal(429, result.Code);
    }

    [Fact]
    public async Task TryMakeMoveAsync_ShouldFail_IfGameNotFound()
    {
        var repo = CreateRepository();
        var move = new PlayerMoveRequest(Guid.NewGuid(), Guid.NewGuid(), 0, 0, 0, 0);

        _eventStoreMock
            .Setup(s => s.GetAllEventsAsync(move.GameId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<IDomainEvent>());

        var result = await repo.TryMakeMoveAsync(move);

        Assert.False(result.IsSuccess);
        Assert.Equal(404, result.Code);
    }

    [Fact]
    public async Task TryClearFinishedGames_ShouldRemoveCompletedGames()
    {
        var repo = CreateRepository();

        var startResult = await repo.TryStartGameAsync();
        Assert.True(startResult.IsSuccess);

        var gameId = startResult.Value.GameId;
        var game = GetPrivateGame(repo, gameId);
        game.ForceStatus(GameStatus.WON);

        var clearResult = await repo.TryClearFinishedGamesAsync();

        Assert.True(clearResult.IsSuccess);
        Assert.Equal(0, repo.GamesNow);
    }

    private GameRoot GetPrivateGame(InMemoryGameRepository repo, Guid id)
    {
        var field = typeof(InMemoryGameRepository).GetField("_games", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var dict = (ConcurrentDictionary<Guid, GameRoot>)field.GetValue(repo)!;
        return dict[id];
    }
}

public static class GameRootTestExtensions
{
    public static void ForceStatus(this GameRoot game, GameStatus status)
    {
        typeof(GameRoot)
            .GetProperty("Status")!
            .SetValue(game, status);
    }
}