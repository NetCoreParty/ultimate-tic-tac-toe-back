using UltimateTicTacToe.Core.Features.Game.Domain.Entities;

namespace UltimateTicTacToe.Core.Tests.Features.Game.Domain.Entities;

public class BigBoardTests
{
    [Fact]
    public void New_BigBoard_Should_Have_All_Empty_MiniBoards()
    {
        // Arrange
        var bigBoard = new BigBoard();

        // Assert
        foreach (var mini in bigBoard.GetMiniBoards())
        {
            Assert.NotNull(mini);
            Assert.Equal(PlayerFigure.None, mini.Winner);
            Assert.False(mini.IsFull);
        }

        Assert.Equal(PlayerFigure.None, bigBoard.Winner);
    }

    [Fact]
    public void TryMakeMove_Should_Succeed_On_Empty_Cell()
    {
        // Arrange
        var bigBoard = new BigBoard();

        // Act
        var moveResult = bigBoard.TryMakeMove(0, 0, 1, 1, PlayerFigure.X);

        // Assert
        Assert.True(moveResult);
        Assert.Equal(PlayerFigure.X, bigBoard.GetMiniBoard(0, 0).GetCell(1, 1).Figure);
    }

    [Fact]
    public void TryMakeMove_Should_Fail_On_Filled_Cell()
    {
        // Arrange
        var bigBoard = new BigBoard();
        bigBoard.TryMakeMove(1, 1, 0, 0, PlayerFigure.X);

        // Act
        var moveResult = bigBoard.TryMakeMove(1, 1, 0, 0, PlayerFigure.O);

        // Assert
        Assert.False(moveResult);
        Assert.Equal(PlayerFigure.X, bigBoard.GetMiniBoard(1, 1).GetCell(0, 0).Figure);
    }

    [Fact]
    public void IsMiniBoardPlayable_Should_Be_True_For_Empty_MiniBoard()
    {
        // Arrange
        var bigBoard = new BigBoard();

        // Act
        var isPlayable = bigBoard.IsMiniBoardPlayable(2, 2);

        // Assert
        Assert.True(isPlayable);
    }

    [Fact]
    public void IsMiniBoardPlayable_Should_Be_False_When_Winner_Exists()
    {
        // Arrange
        var bigBoard = new BigBoard();
        MakeWinningMovesOnMiniBoard(bigBoard, 0, 0, PlayerFigure.X);

        // Act
        var isPlayable = bigBoard.IsMiniBoardPlayable(0, 0);

        // Assert
        Assert.False(isPlayable);
    }

    [Fact]
    public void IsMiniBoardPlayable_Should_Be_False_When_Full()
    {
        // Arrange
        var bigBoard = new BigBoard();

        // Simulate draw scenario
        FillMiniBoardForDraw(bigBoard, 1, 1);

        // Act
        var isPlayable = bigBoard.IsMiniBoardPlayable(1, 1);

        // Assert
        Assert.False(isPlayable);
    }

    [Fact]
    public void Should_Detect_Ultimate_Win_When_MiniBoards_Won()
    {
        // Arrange
        var bigBoard = new BigBoard();

        var winningMiniBoardScenario = new (int boardRow, int boardCol)[]
        {
            // Top row mini-boards
            (0,0), (0,1), (0,2)
        };

        foreach (var (boardRow, boardCol) in winningMiniBoardScenario)
        {
            MakeWinningMovesOnMiniBoard(bigBoard, boardRow, boardCol, PlayerFigure.X);
        }

        // Act
        var failedMove = bigBoard.TryMakeMove(1, 1, 1, 1, PlayerFigure.O); // any move to trigger CheckUltimateWin()

        // Assert
        Assert.Equal(PlayerFigure.X, bigBoard.GetMiniBoard(0, 0).Winner);
        Assert.Equal(PlayerFigure.X, bigBoard.GetMiniBoard(0, 1).Winner);
        Assert.Equal(PlayerFigure.X, bigBoard.GetMiniBoard(0, 2).Winner);
        Assert.Equal(PlayerFigure.X, bigBoard.Winner);
    }

    #region Test Helpers

    private void MakeWinningMovesOnMiniBoard(BigBoard bigBoard, int boardRow, int boardCol, PlayerFigure figure)
    {
        // Let's win top row (cells (0,0), (0,1), (0,2))
        bigBoard.TryMakeMove(boardRow, boardCol, 0, 0, figure);
        bigBoard.TryMakeMove(boardRow, boardCol, 0, 1, figure);
        bigBoard.TryMakeMove(boardRow, boardCol, 0, 2, figure);
    }

    private static void FillMiniBoardForDraw(BigBoard bigBoard, int boardRow, int boardCol)
    {
        // Fill cells to force a draw (no winner)
        var moves = new (int r, int c, PlayerFigure f)[]
        {
            (0,0, PlayerFigure.X), (0,1, PlayerFigure.O), (0,2, PlayerFigure.X),
            (1,0, PlayerFigure.X), (1,1, PlayerFigure.O), (1,2, PlayerFigure.O),
            (2,0, PlayerFigure.O), (2,1, PlayerFigure.X), (2,2, PlayerFigure.X),
        };

        foreach (var (r, c, f) in moves)
        {
            bigBoard.TryMakeMove(boardRow, boardCol, r, c, f);
        }
    }


    #endregion
}