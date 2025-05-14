using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System.Collections.Concurrent;
using UltimateTicTacToe.Core.Configuration;
using UltimateTicTacToe.Core.Features.Game.Domain.Aggregate;
using UltimateTicTacToe.Core.Features.Gameplay;

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
            EventsUntilSnapshot = 5
        });
    }

    private InMemoryGameRepository CreateRepository()
    {
        return new InMemoryGameRepository(
            _eventStoreMock.Object,
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
    public async Task TryMakeMoveAsync_ShouldFail_IfGameNotFound()
    {
        var repo = CreateRepository();
        var move = new PlayerMoveRequest(Guid.NewGuid(), Guid.NewGuid(), 0, 0, 0, 0);

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

        var clearResult = await repo.TryClearFinishedGames();

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