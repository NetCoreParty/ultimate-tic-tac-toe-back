using UltimateTicTacToe.Core.Projections;

namespace UltimateTicTacToe.Core.Features.RealTimeNotification;

public interface IMoveUpdatesNotificationHub
{
    Task NotifyMoveRejectedAsync(string groupName, string? errorDescription, CancellationToken ct = default);

    Task NotifyMoveAppliedAsync(string groupName, MoveResult moveResult, CancellationToken ct = default);

    Task NotifyGameEndedAsync(string groupName, GameEndedNotification payload, CancellationToken ct = default);

    Task NotifyOpponentDisconnectedAsync(string groupName, OpponentPresenceNotification payload, CancellationToken ct = default);

    Task NotifyOpponentReconnectedAsync(string groupName, OpponentPresenceNotification payload, CancellationToken ct = default);
}