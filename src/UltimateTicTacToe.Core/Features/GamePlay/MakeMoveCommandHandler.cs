using MediatR;
using UltimateTicTacToe.Core.Services;

namespace UltimateTicTacToe.Core.Features.GamePlay;

public record MakeMoveCommand(PlayerMoveRequest makeMoveRequest);

public class MakeMoveCommandHandler : IRequestHandler<MakeMoveCommand, Result<>>
{
    private readonly IGameService _gameService;
    private readonly IHubContext<GameHub> _hubContext;

    public MakeMoveCommandHandler(IGameService gameService, IHubContext<GameHub> hubContext)
    {
        _gameService = gameService;
        _hubContext = hubContext;
    }

    public async Task<Result<>> Handle(MakeMoveCommand request, CancellationToken ct)
    {
        var result = await _gameService.ApplyMoveAsync(request.GameId, request.Move);

        var groupName = request.GameId.ToString();

        if (!result.IsSuccess)
        {
            await _hubContext.Clients.Group(groupName)
                .SendAsync("MoveRejected", result.Error, cancellationToken);
            return result;
        }

        await _hubContext.Clients.Group(groupName)
            .SendAsync("MoveApplied", result.Value, cancellationToken);

        return result;
    }
}