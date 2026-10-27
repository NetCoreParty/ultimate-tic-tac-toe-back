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

/// <summary>
/// Shared success payload for HTTP POST /api/game/move and SignalR MoveApplied.
/// </summary>
public record MoveResult(
    Guid GameId,
    GameStatus Status,
    Guid? WinnerId,
    int Version,
    Guid? NextPlayerId,
    PlayerHistoricalMove LastMove);

public record FilteredMovesHistoryResponse(
    Guid GameId,
    List<PlayerHistoricalMove> Moves,
    GameStatus Status,
    Guid? WinnerId);

public record PlayerHistoricalMove(
    Guid PlayerId,
    int MoveId,
    int MiniBoardRowId,
    int MiniBoardColId,
    int CellRowId,
    int CellColId);

public record LeaveGameRequest(Guid GameId);

/// <summary>
/// Outcome: "forfeit" (host left) or "softLeave" (guest left; game continues).
/// </summary>
public record LeaveGameResponse(string Outcome, Guid? WinnerId);

public record GameEndedNotification(Guid GameId, Guid WinnerId, string Reason);

public record OpponentPresenceNotification(Guid GameId, Guid UserId);