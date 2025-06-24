using MediatR;
using UltimateTicTacToe.Core.Projections;
using UltimateTicTacToe.Core.Services;

namespace UltimateTicTacToe.Core.Features.GamePlay;

public record StartGameCommand() : IRequest<Result<StartGameResponse>>;

public class StartGameCommandHandler : IRequestHandler<StartGameCommand, Result<StartGameResponse>>
{
    private readonly IGameRepository _gameRepo;

    public StartGameCommandHandler(IGameRepository gameRepo)
    {
        _gameRepo = gameRepo;
    }

    public async Task<Result<StartGameResponse>> Handle(StartGameCommand request, CancellationToken ct)
    {
        var gameStartedResponse = await _gameRepo.TryStartGameAsync(ct);
        return gameStartedResponse;
    }
}