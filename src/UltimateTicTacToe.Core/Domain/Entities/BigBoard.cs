using UltimateTicTacToe.Core.Domain.Aggregate;
using UltimateTicTacToe.Core.Extensions;
using UltimateTicTacToe.Core.Features.GameSave.Entities;

namespace UltimateTicTacToe.Core.Domain.Entities;

public class BigBoard
{
    private readonly MiniBoard[,] _miniBoards = new MiniBoard[3, 3];
    public PlayerFigure Winner { get; private set; } = PlayerFigure.None;

    public BigBoard()
    {
        for (int r = 0; r < 3; r++)
            for (int c = 0; c < 3; c++)
                _miniBoards[r, c] = new MiniBoard();
    }

    public static BigBoard Restore(List<MiniBoardSnapshot> miniBoardSnapshots)
    {
        var bigBoard = new BigBoard();

        foreach (var miniSnapshot in miniBoardSnapshots)
        {
            var restoredMiniBoard = MiniBoard.Restore(miniSnapshot);
            bigBoard.SetMiniBoard(miniSnapshot.Row, miniSnapshot.Col, restoredMiniBoard);
        }

        return bigBoard;
    }

    public MiniBoard GetMiniBoard(int rowId, int colId)
        => _miniBoards[rowId, colId].Clone();

    public MiniBoard[,] GetMiniBoards() => _miniBoards;

    public bool TryMakeMove(int boardRowId, int boardColId, int cellRowId, int cellColId, PlayerFigure figure)
    {
        var mini = _miniBoards[boardRowId, boardColId];

        if (!mini.TryMakeMove(cellRowId, cellColId, figure))
            return false;

        CheckUltimateWin();

        return true;
    }

    public bool IsMiniBoardPlayable(int row, int col)
    {
        return _miniBoards[row, col].Winner == PlayerFigure.None &&
               !_miniBoards[row, col].IsFull;
    }

    private void CheckUltimateWin()
    {
        var winnersGrid = new PlayerFigure[3, 3];

        for (int r = 0; r < 3; r++)
            for (int c = 0; c < 3; c++)
                winnersGrid[r, c] = _miniBoards[r, c].Winner;

        Winner = winnersGrid.CheckWinner();
    }

    private void SetMiniBoard(int row, int col, MiniBoard miniBoard)
    {
        _miniBoards[row, col] = miniBoard;
    }

    public int GetTotalMoves()
        => _miniBoards.Cast<MiniBoard>()
            .SelectMany(mini => mini.GetCells().Cast<Cell>())
            .Count(cell => cell.Figure != PlayerFigure.None);

    public bool AllMiniBoardsCompleted()
        => _miniBoards.Cast<MiniBoard>()
            .All(mini => mini.IsFull || mini.Winner != PlayerFigure.None);
}