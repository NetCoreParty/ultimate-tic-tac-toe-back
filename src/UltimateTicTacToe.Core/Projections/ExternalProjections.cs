using UltimateTicTacToe.Core.Domain.Aggregate;

namespace UltimateTicTacToe.Core.Projections;

public record StartGameResponse(
    Guid GameId,
    Guid PlayerXId,
    Guid PlayerOId,
    GameStatus GameState
    );

public record PlayerMoveRequest(
    Guid GameId,
    Guid PlayerId,
    int MiniBoardRowId,
    int MiniBoardColId,
    int CellRowId,
    int CellColId
)
{
    public override string ToString() =>
        $"[GameId={GameId}, PlayerId={PlayerId}, MiniBoard=({MiniBoardRowId},{MiniBoardColId}), Cell=({CellRowId},{CellColId})]";
}

public record FilteredMovesHistoryResponse(Guid GameId, List<PlayerHistoricalMove> Moves);

public record PlayerHistoricalMove(
    Guid PlayerId,
    int MoveId,
    int MiniBoardRowId,
    int MiniBoardColId,
    int CellRowId,
    int CellColId);