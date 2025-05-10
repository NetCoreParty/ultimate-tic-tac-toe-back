using UltimateTicTacToe.Core.Features.Game.Domain.Entities;

namespace UltimateTicTacToe.Core.Tests.Features.Game.Domain.Entities;

public class MiniBoardTests
{
    [Fact]
    public void New_Board_Should_Have_All_Empty_Cells()
    {
        // Arrange
        var board = new MiniBoard();

        // Act & Assert
        foreach (var cell in board.GetCells())
            Assert.Equal(PlayerFigure.None, cell.Figure);

        Assert.False(board.IsFull);
        Assert.Equal(PlayerFigure.None, board.Winner);
    }

    [Fact]
    public void TryMakeMove_Should_Set_Symbol_When_Cell_Empty()
    {
        // Arrange
        var board = new MiniBoard();

        // Act
        var moveResult = board.TryMakeMove(1, 1, PlayerFigure.X);

        // Assert
        Assert.True(moveResult);
        Assert.Equal(PlayerFigure.X, board.GetCell(1, 1).Figure);
    }

    [Fact]
    public void TryMakeMove_Should_Fail_When_Cell_Occupied()
    {
        // Arrange
        var board = new MiniBoard();
        board.TryMakeMove(0, 0, PlayerFigure.X);

        // Act
        var moveResult = board.TryMakeMove(0, 0, PlayerFigure.O);

        // Assert
        Assert.False(moveResult);
        Assert.Equal(PlayerFigure.X, board.GetCell(0, 0).Figure);
    }

    [Fact]
    public void TryMakeMove_Should_Fail_When_Winner_Already_Decided()
    {
        // Arrange
        var board = new MiniBoard();

        WinMiniBoardTopRow(board, PlayerFigure.X);

        // Act
        var moveResult = board.TryMakeMove(1, 1, PlayerFigure.O);

        // Assert
        Assert.False(moveResult);
        Assert.Equal(PlayerFigure.X, board.Winner);
    }

    [Fact]
    public void Should_Detect_Win_In_Row()
    {
        // Arrange
        var board = new MiniBoard();

        // Act
        FillMiniBoardRow(board, rowId: 2, PlayerFigure.O);

        // Assert
        Assert.Equal(PlayerFigure.O, board.Winner);
    }

    [Fact]
    public void Should_Detect_Win_In_Column()
    {
        // Arrange
        var board = new MiniBoard();

        // Act
        FillMiniBoardColumn(board, colId: 1, PlayerFigure.X);

        // Assert
        Assert.Equal(PlayerFigure.X, board.Winner);
    }

    [Fact]
    public void Should_Detect_Win_In_Diagonal()
    {
        // Arrange
        var board = new MiniBoard();

        // Act
        WinMiniBoardDiagonal(board, PlayerFigure.O);

        // Assert
        Assert.Equal(PlayerFigure.O, board.Winner);
    }

    [Fact]
    public void Should_Detect_Win_In_Reverse_Diagonal()
    {
        // Arrange
        var board = new MiniBoard();

        // Act
        WinMiniBoardReverseDiagonal(board, PlayerFigure.O);

        // Assert
        Assert.Equal(PlayerFigure.O, board.Winner);
    }

    [Fact]
    public void Board_Should_Be_Full_When_All_Cells_Filled_Like_During_Draw()
    {
        // Arrange
        var board = new MiniBoard();

        // Act
        // A draw scenario (carefully avoiding any winner)
        FillMiniBoardWithDraw(board);

        // Assert
        Assert.True(board.IsFull, "Board should be full after all moves.");
        Assert.Equal(PlayerFigure.None, board.Winner); // No winner expected
    }

    #region Projection Tests

    [Fact]
    public void GetCell_ShouldReturnClonedCell_WithCorrectValues()
    {
        // Arrange
        var board = new MiniBoard();
        board.TryMakeMove(1, 1, PlayerFigure.X);

        // Act
        var cell = board.GetCell(1, 1);

        // Assert
        Assert.Equal(1, cell.RowId);
        Assert.Equal(1, cell.ColId);
        Assert.Equal(PlayerFigure.X, cell.Figure);
    }

    [Fact]
    public void GetCell_ShouldThrow_OnInvalidCoordinates()
    {
        // Arrange
        var board = new MiniBoard();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => board.GetCell(-1, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => board.GetCell(0, 3));
    }

    [Fact]
    public void GetCells_ShouldReturnDeepCloneMatrix()
    {
        // Arrange
        var board = new MiniBoard();
        board.TryMakeMove(0, 0, PlayerFigure.X);
        board.TryMakeMove(1, 1, PlayerFigure.O);

        // Act
        var cells = board.GetCells();

        // Assert
        Assert.Equal(PlayerFigure.X, cells[0, 0].Figure);
        Assert.Equal(PlayerFigure.O, cells[1, 1].Figure);
        Assert.Equal(PlayerFigure.None, cells[2, 2].Figure);

        // Check deep clone (modifying returned array doesn't affect original)
        cells[0, 0].Mark(PlayerFigure.O);
        var originalCell = board.GetCell(0, 0);
        Assert.Equal(PlayerFigure.X, originalCell.Figure); // Original unchanged
    }

    [Fact]
    public void Clone_ShouldReturnEquivalentBoard_WithoutReferenceSharing()
    {
        // Arrange
        var board = new MiniBoard();
        board.TryMakeMove(0, 0, PlayerFigure.X);
        board.TryMakeMove(1, 1, PlayerFigure.O);

        // Act
        var clone = board.Clone();

        // Assert - Check that all cell states are preserved
        Assert.Equal(PlayerFigure.X, clone.GetCell(0, 0).Figure);
        Assert.Equal(PlayerFigure.O, clone.GetCell(1, 1).Figure);
        Assert.Equal(PlayerFigure.None, clone.GetCell(2, 2).Figure);

        // Modify clone and ensure original is unaffected
        var cloneCell = clone.GetCell(0, 0);
        cloneCell.Mark(PlayerFigure.O);

        var originalCell = board.GetCell(0, 0);
        Assert.Equal(PlayerFigure.X, originalCell.Figure);
    }

    #endregion

    #region Test Helpers

    private static void WinMiniBoardTopRow(MiniBoard miniBoard, PlayerFigure figure)
    {
        // Win by filling top row (0,0), (0,1), (0,2)
        miniBoard.TryMakeMove(0, 0, figure);
        miniBoard.TryMakeMove(0, 1, figure);
        miniBoard.TryMakeMove(0, 2, figure);
    }

    private static void FillMiniBoardWithDraw(MiniBoard miniBoard)
    {
        var moves = new (int r, int c, PlayerFigure f)[]
        {
            (0,0, PlayerFigure.X), (0,1, PlayerFigure.O), (0,2, PlayerFigure.X),
            (1,0, PlayerFigure.X), (1,1, PlayerFigure.O), (1,2, PlayerFigure.O),
            (2,0, PlayerFigure.O), (2,1, PlayerFigure.X), (2,2, PlayerFigure.X),
        };

        foreach (var (r, c, figure) in moves)
        {
            miniBoard.TryMakeMove(r, c, figure);
        }
    }

    private static void FillMiniBoardRow(MiniBoard miniBoard, int rowId, PlayerFigure figure)
    {
        for (int col = 0; col < 3; col++)
        {
            miniBoard.TryMakeMove(rowId, col, figure);
        }
    }

    private static void FillMiniBoardColumn(MiniBoard miniBoard, int colId, PlayerFigure figure)
    {
        for (int row = 0; row < 3; row++)
        {
            miniBoard.TryMakeMove(row, colId, figure);
        }
    }

    private static void WinMiniBoardDiagonal(MiniBoard miniBoard, PlayerFigure figure)
    {
        for (int i = 0; i < 3; i++)
        {
            miniBoard.TryMakeMove(i, i, figure);
        }
    }

    private static void WinMiniBoardReverseDiagonal(MiniBoard miniBoard, PlayerFigure figure)
    {
        for (int i = 0; i < 3; i++)
        {
            miniBoard.TryMakeMove(i, 2 - i, figure);
        }
    }

    #endregion

}