namespace UltimateTicTacToe.Core.Features.RealTimeNotification;

public interface IMoveUpdatesNotificationHub
{
    Task NotifyMoveRejectedAsync(string groupName, string? errorDescription, CancellationToken ct = default);

    Task NotifyMoveAppliedAsync(string groupName, bool isMoveApplied, CancellationToken ct = default);
}