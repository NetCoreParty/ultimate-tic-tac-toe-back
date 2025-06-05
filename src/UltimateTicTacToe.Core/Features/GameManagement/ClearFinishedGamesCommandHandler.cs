using MediatR;
using UltimateTicTacToe.Core.Services;

namespace UltimateTicTacToe.Core.Features.GameManagement;

public record ClearFinishedGamesCommand();

public class ClearFinishedGamesCommandHandler : IRequestHandler<ClearFinishedGamesCommand, Result<>>
{
    private readonly IGameService _gameService;

    public ClearFinishedGamesCommandHandler(IGameService gameService)
    {
        _gameService = gameService;
    }

    public async Task<Result<>> Handle(ClearFinishedGamesCommand command, CancellationToken ct)
    {
        
    }
}