using MediatR;
using UltimateTicTacToe.Core.Services;

namespace UltimateTicTacToe.Core.Features.Metrics;

public record GetUnfinishedGamesQuery() : IRequest<Result<int>>;

public class GetUnfinishedGamesQueryHandler : IRequestHandler<GetUnfinishedGamesQuery, Result<int>>
{
    private readonly IGameRepository _gameRepo;

    public GetUnfinishedGamesQueryHandler(IGameRepository gameRepo)
    {
        _gameRepo = gameRepo;
    }

    public Task<Result<int>> Handle(GetUnfinishedGamesQuery request, CancellationToken ct)
    {
        return Task.FromResult(Result<int>.Success(_gameRepo.GamesNow));
    }
}