using UltimateTicTacToe.Core.Domain.Aggregate;
using UltimateTicTacToe.Core.Domain.Entities;
using UltimateTicTacToe.Core.Domain.Events;
using UltimateTicTacToe.Core.Features.GameSaving.Entities.Snapshot;

namespace UltimateTicTacToe.Core.Extensions;

public static class GameRootExtensions
{
    public static GameRootSnapshotProjection ToSnapshot(this GameRoot game)
    {
        var miniBoards = game.Board.GetMiniBoards();
        var mappedMiniBoards = MapMiniBoardsToSnapshot(miniBoards);

        return new GameRootSnapshotProjection
        {
            GameId = game.GameId,
            PlayerXId = game.PlayerXId,
            PlayerOId = game.PlayerOId,
            Status = (int)game.Status,
            WinnerId = game.WinnerId,
            Version = game.Version,
            MiniBoards = mappedMiniBoards,
        };
    }

    private static List<MiniBoardSnapshot> MapMiniBoardsToSnapshot(MiniBoard[,] miniBoards)
    {
        var miniBoardsSnapshot = new List<MiniBoardSnapshot>();

        for (var i = 0; i < miniBoards.GetLength(0); i++)
        {
            for (var j = 0; j < miniBoards.GetLength(1); j++)
            {
                var miniBoard = miniBoards[i, j];

                if (miniBoard.IsEmpty && !miniBoard.IsWon)
                    continue;

                miniBoardsSnapshot.Add(new MiniBoardSnapshot
                {
                    Row = i,
                    Col = j,
                    Winner = miniBoard.Winner != PlayerFigure.None ? miniBoard.Winner : PlayerFigure.None,
                    Cells = MapCellsToSnapshot(miniBoard.GetCells()),
                });
            }
        }

        return miniBoardsSnapshot;
    }

    private static List<CellSnapshot> MapCellsToSnapshot(Cell[,] cells)
    {
        var cellsSnapshot = new List<CellSnapshot>();

        for (var i = 0; i < cells.GetLength(0); i++)
        {
            for (var j = 0; j < cells.GetLength(1); j++)
            {
                var cell = cells[i, j];

                if (cell.Figure == PlayerFigure.None)
                    continue;

                cellsSnapshot.Add(new CellSnapshot
                {
                    Row = cell.RowId,
                    Col = cell.ColId,
                    Figure = cell.Figure.ToString()
                });
            }
        }

        return cellsSnapshot;
    }

    /// TODO: Rewrite this method to use the updated logic, so far we are not using it (moves mapping problem)
    public static GameRoot ToGameRoot_DoesntWorkForNow(this GameRootSnapshotProjection snapshot, List<IDomainEvent> eventsFromStore)
    {
        var gameRoot = GameRoot.CreateNew(
            snapshot.GameId,
            snapshot.PlayerXId,
            snapshot.PlayerOId,
            gameStatus: (GameStatus)snapshot.Status,
            version: snapshot.Version,
            isReplay: true
        );

        foreach (var miniBoard in snapshot.MiniBoards)
        {
            foreach (var cell in miniBoard.Cells)
            {
                gameRoot.Board.TryMakeMove(miniBoard.Row, miniBoard.Col, cell.Row, cell.Col, Enum.Parse<PlayerFigure>(cell.Figure));
            }
        }

        GameRoot.Rehydrate(eventsFromStore, gameRoot);

        return gameRoot;
    }
}