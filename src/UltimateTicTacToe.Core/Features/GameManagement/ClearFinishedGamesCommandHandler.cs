using MediatR;
using UltimateTicTacToe.Core.Services;

namespace UltimateTicTacToe.Core.Features.GameManagement;

public record ClearFinishedGamesCommand() : IRequest<Result<bool>>;

public class ClearFinishedGamesCommandHandler : IRequestHandler<ClearFinishedGamesCommand, Result<bool>>
{
    private readonly IGameRepository _gameRepo;

    public ClearFinishedGamesCommandHandler(IGameRepository gameRepo)
    {
        _gameRepo = gameRepo;
    }

    public async Task<Result<bool>> Handle(ClearFinishedGamesCommand command, CancellationToken ct)
    {
        var result = await _gameRepo.TryClearFinishedGamesAsync(ct);

        return result;
    }
}