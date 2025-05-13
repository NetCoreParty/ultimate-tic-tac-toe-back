using UltimateTicTacToe.Core.Features.Game.Domain.Aggregate;
using UltimateTicTacToe.Core.Features.Game.Domain.Entities;
using UltimateTicTacToe.Core.Features.Game.Domain.Events;
using UltimateTicTacToe.Core.Features.Game.Domain.Exceptions;
using UltimateTicTacToe.Core.Tests.Unit.Infrastructure;

namespace UltimateTicTacToe.Core.Tests.Unit.Features.Game.Domain.Aggregate;

public class GameRootTests
{
    [Fact]
    public void CreateNew_ShouldInitializeGameProperly()
    {
        // Arrange
        var gameId = Guid.NewGuid();
        var playerXId = Guid.NewGuid();
        var playerOId = Guid.NewGuid();

        // Act
        var game = GameRoot.CreateNew(gameId, playerXId, playerOId);

        // Assert
        Assert.Equal(gameId, game.GameId);
        Assert.Equal(playerXId, game.PlayerXId);
        Assert.Equal(playerOId, game.PlayerOId);
        Assert.NotNull(game.Board);
        Assert.Equal(GameStatus.IN_PROGRESS, game.Status);

        foreach (var mini in game.Board.GetMiniBoards())
        {
            foreach (var cell in mini.GetCells())
            {
                Assert.True(cell.Figure == PlayerFigure.None, $"Cell at column: {cell.ColId}, row: {cell.RowId} should be empty, now figure is: {cell.Figure}");
            }

            Assert.NotNull(mini);
            Assert.Equal(PlayerFigure.None, mini.Winner);
            Assert.False(mini.IsFull);
        }
    }

    [Fact]
    public void PlayMove_ShouldFail_WhenNotInProgress()
    {
        // Arrange
        var playerXId = Guid.NewGuid();
        var playerOId = Guid.NewGuid();
        var game = GameRoot.CreateNew(Guid.NewGuid(), playerXId, playerOId);

        game.ForceSetWinner(playerXId);

        // Now game status should be WON
        Assert.Equal(GameStatus.WON, game.Status);

        // Act & Assert
        Assert.Throws<GameNotInProgressException>(() =>
            game.PlayMove(playerOId, 2, 2, 1, 1));
    }

    [Fact]
    public void PlayMove_ShouldFail_WhenNotPlayersTurn()
    {
        // Arrange
        var playerXId = Guid.NewGuid();
        var playerOId = Guid.NewGuid();
        var game = GameRoot.CreateNew(Guid.NewGuid(), playerXId, playerOId);

        // Wrong player tries to move
        var wrongPlayerId = Guid.NewGuid();

        // Act & Assert
        Assert.Throws<NotYourTurnException>(() =>
            game.PlayMove(wrongPlayerId, 0, 0, 0, 0));
    }

    [Fact]
    public void PlayMove_ShouldSucceed_WhenValidMove()
    {
        // Arrange
        var playerXId = Guid.NewGuid();
        var playerOId = Guid.NewGuid();
        var game = GameRoot.CreateNew(Guid.NewGuid(), playerXId, playerOId);

        // Act
        game.PlayMove(playerXId, 0, 0, 0, 0);
        game.PlayMove(playerOId, 0, 0, 1, 0);

        // Assert
        var board = game.Board;
        var mini = board.GetMiniBoard(0, 0);

        Assert.Equal(PlayerFigure.X, mini.GetCells()[0, 0].Figure);
        Assert.Equal(PlayerFigure.O, mini.GetCells()[1, 0].Figure);
    }

    #region Check Right Events Emitting

    [Fact]
    public void PlayMove_ShouldEmit_CellMarkedEvent_WhenMovePlayed()
    {
        // Arrange
        var playerXId = Guid.NewGuid();
        var playerOId = Guid.NewGuid();
        var game = GameRoot.CreateNew(Guid.NewGuid(), playerXId, playerOId);

        // Act
        game.PlayMove(playerXId, 0, 0, 0, 0);

        // Assert
        Assert.Single(game.UncommittedChanges.OfType<CellMarkedEvent>());
    }

    [Fact]
    public void PlayMove_ShouldEmit_MiniBoardWonEvent_WhenMiniBoardIsWon()
    {
        // Arrange
        var playerXId = Guid.NewGuid();
        var playerOId = Guid.NewGuid();

        var game = GameRoot.CreateNew(Guid.NewGuid(), playerXId, playerOId);

        game.SimulateMiniBoardWin(playerXId, playerOId);

        // Act
        var miniBoardEvent = game.UncommittedChanges.OfType<MiniBoardWonEvent>().FirstOrDefault();

        // Assert
        Assert.NotNull(miniBoardEvent);
        Assert.Single(game.UncommittedChanges.OfType<MiniBoardWonEvent>());
        Assert.Equal(playerXId, miniBoardEvent.WinnerId);
        Assert.Equal(0, miniBoardEvent.BoardRowId);
        Assert.Equal(0, miniBoardEvent.BoardColId);
    }

    [Fact]
    public void PlayMove_ShouldEmit_MiniBoardDrawnEvent_WhenMiniBoard_WithoutWinner()
    {
        // Arrange
        var playerXId = Guid.NewGuid();
        var playerOId = Guid.NewGuid();

        var game = GameRoot.CreateNew(Guid.NewGuid(), playerXId, playerOId);

        game.ForceMiniBoardDraw(miniBoardRow: 0, miniBoardCol: 0);

        // Act
        var miniBoardEvent = game.UncommittedChanges.OfType<MiniBoardDrawnEvent>().FirstOrDefault();

        // Assert
        Assert.NotNull(miniBoardEvent);
        Assert.Single(game.UncommittedChanges.OfType<MiniBoardDrawnEvent>());
        Assert.Equal(0, miniBoardEvent.BoardRowId);
        Assert.Equal(0, miniBoardEvent.BoardColId);
    }

    [Fact]
    public void PlayMove_ShouldEmit_FullGameWonEvent_WhenGameIsWon()
    {
        // Arrange
        var playerXId = Guid.NewGuid();
        var playerOId = Guid.NewGuid();

        var game = GameRoot.CreateNew(Guid.NewGuid(), playerXId, playerOId);

        // Act
        game.ForceSetStatus(GameStatus.WON);
        var fullGameWonEvent = game.UncommittedChanges.OfType<FullGameWonEvent>().FirstOrDefault();

        // Assert
        Assert.Single(game.UncommittedChanges.OfType<FullGameWonEvent>());
        Assert.NotNull(fullGameWonEvent);
        Assert.Equal(playerXId, fullGameWonEvent.WinnerId);
    }

    [Fact]
    public void PlayMove_ShouldEmit_GameDrawnEvent_WhenGameIsDrawn()
    {
        // Arrange
        var playerXId = Guid.NewGuid();
        var playerOId = Guid.NewGuid();
        var gameId = Guid.NewGuid();
        var game = GameRoot.CreateNew(gameId, playerXId, playerOId);

        // Act
        game.ForceSetStatus(GameStatus.DRAW);
        var gameDrawnEvent = game.UncommittedChanges.OfType<GameDrawnEvent>().FirstOrDefault();

        // Assert
        Assert.Single(game.UncommittedChanges.OfType<GameDrawnEvent>());
        Assert.NotNull(gameDrawnEvent);
        Assert.Equal(92, game.UncommittedChanges.Count);
        Assert.Equal(GameStatus.DRAW, game.Status);
        Assert.Equal(gameId, gameDrawnEvent.GameId);
    }

    #endregion

    #region Check Rehydration

    [Fact]
    public void Rehydrate_ShouldRebuildGameProperly()
    {
        // Arrange
        var gameId = Guid.NewGuid();
        var playerXId = Guid.NewGuid();
        var playerOId = Guid.NewGuid();

        var events = new DomainEventBase[]
        {
            new GameCreatedEvent(gameId, playerXId, playerOId),
            new CellMarkedEvent(gameId, playerXId, 0, 0, 0, 0, PlayerFigure.X),
            new CellMarkedEvent(gameId, playerOId, 0, 0, 0, 1, PlayerFigure.O)
        };

        // Act
        var game = GameRoot.Rehydrate(events, null);

        // Assert
        var mini = game.Board.GetMiniBoard(0, 0);
        Assert.True(game.UncommittedChanges.Count == 0);
        Assert.Equal(PlayerFigure.X, mini.GetCells()[0, 0].Figure);
        Assert.Equal(PlayerFigure.O, mini.GetCells()[0, 1].Figure);
    }

    #endregion
}