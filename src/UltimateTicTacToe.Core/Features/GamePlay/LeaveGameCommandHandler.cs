using MediatR;
using UltimateTicTacToe.Core.Features.RealTimeNotification;
using UltimateTicTacToe.Core.Projections;
using UltimateTicTacToe.Core.Services;

namespace UltimateTicTacToe.Core.Features.GamePlay;

public record LeaveGameCommand(Guid GameId, Guid UserId) : IRequest<Result<LeaveGameResponse>>;

public class LeaveGameCommandHandler : IRequestHandler<LeaveGameCommand, Result<LeaveGameResponse>>
{
    private readonly IGameRepository _gameRepo;
    private readonly IMoveUpdatesNotificationHub _realTimeNotifier;

    public LeaveGameCommandHandler(IGameRepository gameRepo, IMoveUpdatesNotificationHub realTimeNotifier)
    {
        _gameRepo = gameRepo;
        _realTimeNotifier = realTimeNotifier;
    }

    public async Task<Result<LeaveGameResponse>> Handle(LeaveGameCommand request, CancellationToken ct)
    {
        var result = await _gameRepo.TryLeaveGameAsync(request.GameId, request.UserId, ct);
        if (!result.IsSuccess || result.Value is null)
            return result;

        var groupName = request.GameId.ToString();
        var leave = result.Value;

        if (leave.Outcome == "forfeit" && leave.WinnerId is Guid winnerId)
        {
            await _realTimeNotifier.NotifyGameEndedAsync(
                groupName,
                new GameEndedNotification(request.GameId, winnerId, "forfeit"),
                ct);
        }
        else if (leave.Outcome == "softLeave")
        {
            await _realTimeNotifier.NotifyOpponentDisconnectedAsync(
                groupName,
                new OpponentPresenceNotification(request.GameId, request.UserId),
                ct);
        }

        return result;
    }
}
