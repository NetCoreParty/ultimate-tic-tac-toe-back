using UltimateTicTacToe.Core.Features.Game.Domain.Entities;

namespace UltimateTicTacToe.Core.Tests.Unit.Features.Game.Domain.Entities;

public class CellTests
{
    [Fact]
    public void Constructor_ShouldInitializeCorrectly()
    {
        // Arrange
        int row = 1, col = 2;

        // Act
        var cell = new Cell(row, col);

        // Assert
        Assert.Equal(row, cell.RowId);
        Assert.Equal(col, cell.ColId);
        Assert.Equal(PlayerFigure.None, cell.Figure);
    }

    [Fact]
    public void Mark_ShouldSetFigureCorrectly()
    {
        // Arrange
        var cell = new Cell(0, 0);

        // Act
        cell.Mark(PlayerFigure.X);

        // Assert
        Assert.Equal(PlayerFigure.X, cell.Figure);
    }

    [Fact]
    public void Clone_ShouldReturnNewInstanceWithSameValues()
    {
        // Arrange
        var original = new Cell(2, 1);
        original.Mark(PlayerFigure.O);

        // Act
        var clone = original.Clone();

        // Assert
        Assert.NotSame(original, clone);
        Assert.Equal(original.RowId, clone.RowId);
        Assert.Equal(original.ColId, clone.ColId);
        Assert.Equal(original.Figure, clone.Figure);
    }

    [Fact]
    public void Clone_ShouldNotAffectOriginal_WhenModified()
    {
        // Arrange
        var original = new Cell(0, 0);
        original.Mark(PlayerFigure.X);
        var clone = original.Clone();

        // Act
        clone.Mark(PlayerFigure.O);

        // Assert
        Assert.Equal(PlayerFigure.X, original.Figure); // original should remain unchanged
        Assert.Equal(PlayerFigure.O, clone.Figure);    // clone should be updated
    }
}