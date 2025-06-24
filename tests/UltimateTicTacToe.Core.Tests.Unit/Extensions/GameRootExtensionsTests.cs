using UltimateTicTacToe.Core.Domain.Aggregate;
using UltimateTicTacToe.Core.Domain.Entities;
using UltimateTicTacToe.Core.Domain.Events;
using UltimateTicTacToe.Core.Features.GameSave;
using UltimateTicTacToe.Core.Features.GameSave.Entities;

namespace UltimateTicTacToe.Core.Tests.Unit.Extensions;

public class GameRootExtensionsTests
{
    [Fact]
    public void Cell_ShouldRestoreCellCorrectly()
    {
        var cell = Cell.Restore(1, 2, PlayerFigure.O);

        Assert.Equal(1, cell.RowId);
        Assert.Equal(2, cell.ColId);
        Assert.Equal(PlayerFigure.O, cell.Figure);
    }

    [Fact]
    public void MiniBoard_ShouldRestoreMiniBoardCorrectly()
    {
        var snapshot = new MiniBoardSnapshot
        {
            Row = 0,
            Col = 0,
            Winner = PlayerFigure.X,
            Cells = new List<CellSnapshot>
            {
                new CellSnapshot { Row = 0, Col = 0, Figure = PlayerFigure.X.ToString() },
                new CellSnapshot { Row = 1, Col = 1, Figure = PlayerFigure.O.ToString() }
            }
        };

        var board = MiniBoard.Restore(snapshot);

        Assert.Equal(PlayerFigure.X, board.Winner);
        Assert.Equal(PlayerFigure.X, board.GetCell(0, 0).Figure);
        Assert.Equal(PlayerFigure.O, board.GetCell(1, 1).Figure);
    }

    [Fact]
    public void BigBoard_ShouldRestoreBigBoardCorrectly()
    {
        var miniBoardSnapshot = new MiniBoardSnapshot
        {
            Row = 0,
            Col = 0,
            Winner = PlayerFigure.O,
            Cells = new List<CellSnapshot>
            {
                // Diagonal Win (O)
                new CellSnapshot { Row = 0, Col = 1, Figure = PlayerFigure.X.ToString() },
                new CellSnapshot { Row = 0, Col = 0, Figure = PlayerFigure.O.ToString() },
                new CellSnapshot { Row = 2, Col = 1, Figure = PlayerFigure.X.ToString() },
                new CellSnapshot { Row = 1, Col = 1, Figure = PlayerFigure.O.ToString() },
                new CellSnapshot { Row = 0, Col = 2, Figure = PlayerFigure.X.ToString() },
                new CellSnapshot { Row = 2, Col = 2, Figure = PlayerFigure.O.ToString() }
            }
        };

        var bigBoard = BigBoard.Restore(new List<MiniBoardSnapshot> { miniBoardSnapshot });

        var miniBoard = bigBoard.GetMiniBoard(0, 0);

        Assert.True(miniBoard.IsWon);
        Assert.False(miniBoard.IsEmpty);
        Assert.Equal(PlayerFigure.O, miniBoard.Winner);

        Assert.Equal(PlayerFigure.O, miniBoard.GetCell(0, 0).Figure);
        Assert.Equal(PlayerFigure.O, miniBoard.GetCell(1, 1).Figure);
        Assert.Equal(PlayerFigure.O, miniBoard.GetCell(2, 2).Figure);

        Assert.Equal(PlayerFigure.X, miniBoard.GetCell(0, 1).Figure);
        Assert.Equal(PlayerFigure.X, miniBoard.GetCell(2, 1).Figure);
        Assert.Equal(PlayerFigure.X, miniBoard.GetCell(0, 2).Figure);
    }

    [Fact]
    public void ToSnapshot_Should_MapAllFields_Correctly()
    {
        // Arrange
        var gameId = Guid.NewGuid();
        var playerX = Guid.NewGuid();
        var playerO = Guid.NewGuid();

        var game = GameRoot.CreateNew(gameId, playerX, playerO);

        // Simulate a few moves
        game.PlayMove(game.PlayerXId, 0, 0, 0, 0);
        game.PlayMove(game.PlayerOId, 0, 0, 0, 1);

        // Act
        var snapshot = game.ToSnapshot();

        // Assert
        Assert.Equal(game.GameId, snapshot.GameId);
        Assert.Equal(game.PlayerXId, snapshot.PlayerXId);
        Assert.Equal(game.PlayerOId, snapshot.PlayerOId);
        Assert.Equal((int)game.Status, snapshot.Status);
        Assert.Equal(game.WinnerId, snapshot.WinnerId);
        Assert.Equal(game.Version, snapshot.Version);
        Assert.Single(snapshot.MiniBoards); // Players filled only two cells in one mini board, so only one mini board should be present

        var affectedMiniBoard = snapshot.MiniBoards.First(b => b.Row == 0 && b.Col == 0);
        Assert.Equal(2, affectedMiniBoard.Cells.Count);
        Assert.Equal(PlayerFigure.X.ToString(), affectedMiniBoard.Cells.First(c => c.Row == 0 && c.Col == 0).Figure);
        Assert.Equal(PlayerFigure.O.ToString(), affectedMiniBoard.Cells.First(c => c.Row == 0 && c.Col == 1).Figure);
    }

    [Fact]
    public void ToGameRoot_Should_ReconstructGame_Correctly()
    {
        var snapshot = new GameRootSnapshotProjection
        {
            GameId = Guid.NewGuid(),
            PlayerXId = Guid.NewGuid(),
            PlayerOId = Guid.NewGuid(),
            Status = (int)GameStatus.IN_PROGRESS,
            Version = 5,
            MiniBoards = new List<MiniBoardSnapshot>
            {
                new MiniBoardSnapshot
                {
                    Row = 1,
                    Col = 2,
                    Winner = PlayerFigure.X,
                    Cells = new List<CellSnapshot>
                    {
                        // Reversed Dagonal Win (X)
                        new CellSnapshot { Row = 2, Col = 0, Figure = PlayerFigure.X.ToString() },
                        new CellSnapshot { Row = 0, Col = 0, Figure = PlayerFigure.O.ToString() },
                        new CellSnapshot { Row = 1, Col = 1, Figure = PlayerFigure.X.ToString() },
                        new CellSnapshot { Row = 1, Col = 0, Figure = PlayerFigure.O.ToString() },
                        new CellSnapshot { Row = 0, Col = 2, Figure = PlayerFigure.X.ToString() }
                    }
                }
            }
        };

        var events = new List<IDomainEvent>(); // Empty for now

        var restoredGameRoot = snapshot.ToGameRoot(events);

        Assert.Equal(snapshot.GameId, restoredGameRoot.GameId);
        Assert.Equal(snapshot.PlayerXId, restoredGameRoot.PlayerXId);
        Assert.Equal(snapshot.PlayerOId, restoredGameRoot.PlayerOId);
        Assert.Equal((GameStatus)snapshot.Status, restoredGameRoot.Status);
        Assert.Equal(snapshot.Version, restoredGameRoot.Version);

        var miniBoard = restoredGameRoot.Board.GetMiniBoard(1, 2);
        Assert.Equal(PlayerFigure.X, miniBoard.Winner);
        Assert.Equal(PlayerFigure.X, miniBoard.GetCell(2, 0).Figure);
        Assert.Equal(PlayerFigure.O, miniBoard.GetCell(0, 0).Figure);
        Assert.Equal(PlayerFigure.X, miniBoard.GetCell(1, 1).Figure);
        Assert.Equal(PlayerFigure.O, miniBoard.GetCell(1, 0).Figure);
        Assert.Equal(PlayerFigure.X, miniBoard.GetCell(0, 2).Figure);
    }
}