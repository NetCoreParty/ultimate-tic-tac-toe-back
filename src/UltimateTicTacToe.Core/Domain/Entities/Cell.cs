namespace UltimateTicTacToe.Core.Domain.Entities;

public class Cell
{
    public int RowId { get; set; }
    public int ColId { get; set; }
    public PlayerFigure Figure { get; internal set; } = PlayerFigure.None;

    public Cell(int rowId, int colId)
    {
        RowId = rowId;
        ColId = colId;
    }

    public static Cell Restore(int rowId, int colId, PlayerFigure figure) 
        => new Cell(rowId, colId, figure);

    private Cell(int rowId, int colId, PlayerFigure figure)
    {
        RowId = rowId;
        ColId = colId;
        Figure = figure;
    }

    public void Mark(PlayerFigure symbol)
    {
        Figure = symbol;
    }

    public Cell Clone()
    {
        var clone = new Cell(RowId, ColId, Figure);

        return clone;
    }
}