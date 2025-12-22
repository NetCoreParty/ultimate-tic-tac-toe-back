using Moq;
using Microsoft.Extensions.Options;
using UltimateTicTacToe.Core.Domain.Aggregate;
using UltimateTicTacToe.Core.Domain.Entities;
using UltimateTicTacToe.Core.Configuration;
using UltimateTicTacToe.Core.Features.GameSaving;
using UltimateTicTacToe.Core.Services;
using UltimateTicTacToe.Core.Tests.Unit.Infrastructure;

namespace UltimateTicTacToe.Core.Tests.Unit.Features.Game.Domain.Services;

public class StateSnapshotStoreTests
{
    private readonly Mock<IEventStore> _eventStoreMock;
    private readonly IStateSnapshotStore _sut = new StateSnapshotStore(Options.Create(new GameplaySettings { EventsUntilSnapshot = 20 }));
    
    public StateSnapshotStoreTests()
    {
        _eventStoreMock = new Mock<IEventStore>();
    }

    [Fact]
    public async Task TryGetLatestSnapshotVersionAsync_ShouldReturnNull_IfNotExists()
    {
        // Arrange & Act
        var result = await _sut.TryGetLatestSnapshotVersionAsync(Guid.NewGuid());
        
        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task TryCreateSnapshotAsync_ShouldStoreSnapshot_WhenGameWon()
    {
        // Arrange
        var game = GameRoot.CreateNew(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        game.ForceSetStatus(GameStatus.WON);

        // Act
        var version = await _sut.TryCreateSnapshotAsync(game);

        // Assert
        Assert.NotNull(version);
        Assert.Equal(23, version);
    }

    [Fact]
    public async Task TryLoadGameAsync_WhenSnapshotExists_ShouldRehydrateFromSnapshot()
    {
        // Arrange
        var gameId = Guid.NewGuid();
        var gameRoot = GameRoot.CreateNew(gameId, Guid.NewGuid(), Guid.NewGuid());
        
        // Play and create a snapshot with MiniBoard (0,0) win
        gameRoot.SimulateMiniBoardWin(gameRoot.PlayerXId, gameRoot.PlayerOId);        
        var currentSnapshotVersion = await _sut.TryCreateSnapshotAsync(gameRoot);

        // Keep playing in the next mini board, + 2 uncommitted events
        gameRoot.PlayMove(gameRoot.PlayerOId, 0, 1, 0, 0);
        gameRoot.PlayMove(gameRoot.PlayerXId, 0, 1, 0, 1);
        var eventsSince = 2;
        var eventsSinceSnapshot = gameRoot.UncommittedChanges
                .Skip(currentSnapshotVersion.Value)
                .Take(eventsSince)
                .ToList();

        _eventStoreMock.Setup(s => s.GetEventsAfterVersionAsync(gameId, currentSnapshotVersion.Value, CancellationToken.None))
            .ReturnsAsync(eventsSinceSnapshot);

        // Act
        var rehydratedGameState = await _sut.TryLoadGameAsync(gameId, _eventStoreMock.Object, CancellationToken.None);
        
        // Assert
        Assert.NotNull(currentSnapshotVersion);
        Assert.NotNull(rehydratedGameState);
        Assert.Equal(gameId, rehydratedGameState.GameId);
        Assert.Equal(9, rehydratedGameState.Version);
        
        var miniBoardWithChanges = rehydratedGameState.Board.GetMiniBoard(0, 1);
        Assert.False(miniBoardWithChanges.IsEmpty);
        Assert.Equal(PlayerFigure.O, miniBoardWithChanges.GetCell(0, 0).Figure);
        Assert.Equal(PlayerFigure.X, miniBoardWithChanges.GetCell(0, 1).Figure);
    }

    [Fact]
    public async Task TryLoadGameAsync_WithoutSnapshot_ShouldReplayFromStart()
    {
        // Arrange
        var gameId = Guid.NewGuid();
        var gameRoot = GameRoot.CreateNew(gameId, Guid.NewGuid(), Guid.NewGuid());

        gameRoot.SimulateMiniBoardWin(gameRoot.PlayerXId, gameRoot.PlayerOId);

        _eventStoreMock.Setup(s => s.GetAllEventsAsync(gameId, CancellationToken.None)).ReturnsAsync(gameRoot.UncommittedChanges.ToList());

        // Act
        var rehydratedGameState = await _sut.TryLoadGameAsync(gameId, _eventStoreMock.Object, CancellationToken.None);

        // Assert
        Assert.NotNull(rehydratedGameState);
        Assert.Equal(gameId, rehydratedGameState.GameId);
        Assert.Equal(7, rehydratedGameState.Version);
        Assert.Equal(0, rehydratedGameState.UncommittedChanges.Count);
    }
}