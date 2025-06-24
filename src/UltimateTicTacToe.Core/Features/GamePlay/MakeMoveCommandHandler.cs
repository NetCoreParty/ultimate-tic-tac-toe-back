using MediatR;
using UltimateTicTacToe.Core.Services;
using Microsoft.AspNetCore.SignalR;
using UltimateTicTacToe.Core.Features.RealTimeMoveUpdates;
using UltimateTicTacToe.Core.Projections;

namespace UltimateTicTacToe.Core.Features.GamePlay;

public record MakeMoveCommand(PlayerMoveRequest makeMoveRequest) : IRequest<Result<bool>>;

public class MakeMoveCommandHandler : IRequestHandler<MakeMoveCommand, Result<bool>>
{
    private readonly IGameRepository _gameRepo;
    private readonly IHubContext<MoveUpdatesHub> _hubContext;

    public MakeMoveCommandHandler(IGameRepository gameRepo, IHubContext<MoveUpdatesHub> hubContext)
    {
        _gameRepo = gameRepo;
        _hubContext = hubContext;
    }

    public async Task<Result<bool>> Handle(MakeMoveCommand request, CancellationToken ct)
    {
        var result = await _gameRepo.TryMakeMoveAsync(request.makeMoveRequest, ct);
        var groupName = request.makeMoveRequest.GameId.ToString();

        if (!result.IsSuccess)
        {
            await _hubContext.Clients.Group(groupName)
                .SendAsync("MoveRejected", result.Error, ct);

            return result;
        }

        await _hubContext.Clients.Group(groupName)
            .SendAsync("MoveApplied", result.Value, ct);

        return result;
    }
}