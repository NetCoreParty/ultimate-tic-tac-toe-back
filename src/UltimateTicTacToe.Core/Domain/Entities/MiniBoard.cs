using UltimateTicTacToe.Core.Extensions;
using UltimateTicTacToe.Core.Features.GameSave.Entities;

namespace UltimateTicTacToe.Core.Domain.Entities;

public class MiniBoard
{
    private readonly Cell[,] _cells = new Cell[3, 3];
    public PlayerFigure Winner { get; private set; }
    public bool IsEmpty => _cells.Cast<Cell>().All(c => c.Figure == PlayerFigure.None);
    public bool IsFull => _cells.Cast<Cell>().All(c => c.Figure != PlayerFigure.None);
    public bool IsWon => Winner != PlayerFigure.None;
    public bool IsDraw => IsFull && !IsWon;

    public MiniBoard(PlayerFigure winner = PlayerFigure.None)
    {
        for (int r = 0; r < 3; r++)
            for (int c = 0; c < 3; c++)
                _cells[r, c] = new Cell(r, c);

        Winner = winner;
    }

    public static MiniBoard Restore(MiniBoardSnapshot miniBoardSnapshot)
    {
        var miniBoard = new MiniBoard();

        foreach (var cellSnapshot in miniBoardSnapshot.Cells)
        {
            var cell = Cell.Restore(cellSnapshot.Row, cellSnapshot.Col, Enum.Parse<PlayerFigure>(cellSnapshot.Figure));
            miniBoard.SetCell(cellSnapshot.Row, cellSnapshot.Col, cell);
        }

        miniBoard.SetWinner(miniBoardSnapshot.Winner ?? PlayerFigure.None);

        return miniBoard;
    }

    private void SetCell(int row, int col, Cell cell)
    {
        _cells[row, col] = cell;
    }

    private void SetWinner(PlayerFigure winner)
    {
        Winner = winner;
    }

    public bool TryMakeMove(int rowId, int colId, PlayerFigure figure)
    {
        if (Winner != PlayerFigure.None || _cells[rowId, colId].Figure != PlayerFigure.None)
            return false;

        _cells[rowId, colId].Mark(figure);

        CheckWin();

        return true;
    }

    public Cell GetCell(int rowId, int colId)
    {
        if (rowId < 0 || rowId > 2 || colId < 0 || colId > 2)
            throw new ArgumentOutOfRangeException("Invalid cell coordinates.");

        return _cells[rowId, colId].Clone();
    }

    public Cell[,] GetCells()
    {
        var cells = new Cell[3, 3];

        for (int r = 0; r < 3; r++)
            for (int c = 0; c < 3; c++)
                cells[r, c] = _cells[r, c].Clone();

        return cells;
    }

    /// <summary>
    /// For absolute safety from unexpected changes, when we need to clone the board.
    /// </summary>
    public MiniBoard Clone()
    {
        var newBoard = new MiniBoard();

        for (int r = 0; r < 3; r++)
        {
            for (int c = 0; c < 3; c++)
            {
                var symbol = _cells[r, c].Figure;
                if (symbol != PlayerFigure.None)
                {
                    newBoard.TryMakeMove(r, c, symbol);
                }
            }
        }

        return newBoard;
    }

    private void CheckWin()
    {
        var grid = new PlayerFigure[3, 3];

        for (int r = 0; r < 3; r++)
            for (int c = 0; c < 3; c++)
                grid[r, c] = _cells[r, c].Figure;

        Winner = grid.CheckWinner();
    }
}