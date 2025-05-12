using Moq;
using System.Linq;
using UltimateTicTacToe.Core.Features.Game.Domain.Aggregate;
using UltimateTicTacToe.Core.Features.Game.Domain.Entities;
using UltimateTicTacToe.Core.Features.Game.Domain.Entities.Snapshot;
using UltimateTicTacToe.Core.Features.Game.Domain.Events;
using UltimateTicTacToe.Core.Services;
using UltimateTicTacToe.Core.Tests.Infrastructure;

namespace UltimateTicTacToe.Core.Tests.Features.Game.Domain.Services;

public class StateSnapshotStoreTests
{
    private readonly Mock<IEventStore> _eventStoreMock;
    private readonly IStateSnapshotStore _sut = new StateSnapshotStore();
    
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

    [Fact(Skip = "ToGameRoot() method from _sut.TryLoadGameAsync() cant be used, because we dont know exactly the order of moves to recreate a snapshot from a certain point, think about it later")]
    public async Task TryLoadGameAsync_WhenSnapshotExists_ShouldRehydrateFromSnapshot()
    {
        // Arrange
        var gameId = Guid.NewGuid();
        var gameRoot = GameRoot.CreateNew(gameId, Guid.NewGuid(), Guid.NewGuid());
        
        // Play and create a snapshot
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

        _eventStoreMock.Setup(s => s.GetEventsAfterVersionAsync(gameId, currentSnapshotVersion.Value))
            .ReturnsAsync(eventsSinceSnapshot);

        // Act
        var rehydratedGameState = await _sut.TryLoadGameAsync(gameId, _eventStoreMock.Object);
        
        // Assert
        Assert.NotNull(currentSnapshotVersion);
        Assert.NotNull(rehydratedGameState);
        Assert.Equal(gameId, rehydratedGameState.GameId);
        Assert.Equal(9, rehydratedGameState.Version);
    }

    [Fact]
    public async Task TryLoadGameAsync_WithoutSnapshot_ShouldReplayFromStart()
    {
        // Arrange
        var gameId = Guid.NewGuid();
        var gameRoot = GameRoot.CreateNew(gameId, Guid.NewGuid(), Guid.NewGuid());

        gameRoot.SimulateMiniBoardWin(gameRoot.PlayerXId, gameRoot.PlayerOId);

        _eventStoreMock.Setup(s => s.GetAllEventsAsync(gameId)).ReturnsAsync(gameRoot.UncommittedChanges.ToList());

        // Act
        var rehydratedGameState = await _sut.TryLoadGameAsync(gameId, _eventStoreMock.Object);

        // Assert
        Assert.NotNull(rehydratedGameState);
        Assert.Equal(gameId, rehydratedGameState.GameId);
        Assert.Equal(7, rehydratedGameState.Version);
        Assert.Equal(0, rehydratedGameState.UncommittedChanges.Count);
    }
}