using MediatR;
using UltimateTicTacToe.Core.Services;
using UltimateTicTacToe.Core.Projections;
using UltimateTicTacToe.Core.Features.RealTimeNotification;

namespace UltimateTicTacToe.Core.Features.GamePlay;

public record MakeMoveCommand(PlayerMoveRequest makeMoveRequest) : IRequest<Result<MoveResult>>;

public class MakeMoveCommandHandler : IRequestHandler<MakeMoveCommand, Result<MoveResult>>
{
    private readonly IGameRepository _gameRepo;
    private readonly IMoveUpdatesNotificationHub _realTimeNotifier;

    public MakeMoveCommandHandler(IGameRepository gameRepo, IMoveUpdatesNotificationHub realTimeNotifier)
    {
        _gameRepo = gameRepo;
        _realTimeNotifier = realTimeNotifier;
    }

    public async Task<Result<MoveResult>> Handle(MakeMoveCommand request, CancellationToken ct)
    {
        var result = await _gameRepo.TryMakeMoveAsync(request.makeMoveRequest, ct);
        var groupName = request.makeMoveRequest.GameId.ToString();

        if (!result.IsSuccess || result.Value is null)
        {
            await _realTimeNotifier.NotifyMoveRejectedAsync(groupName, result.Error, ct);
            return result;
        }

        await _realTimeNotifier.NotifyMoveAppliedAsync(groupName, result.Value, ct);
        return result;
    }
}
