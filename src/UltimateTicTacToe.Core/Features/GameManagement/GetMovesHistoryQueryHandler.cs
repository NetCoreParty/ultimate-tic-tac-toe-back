using MediatR;
using UltimateTicTacToe.Core.Projections;
using UltimateTicTacToe.Core.Services;

namespace UltimateTicTacToe.Core.Features.GameManagement;

public record GetMovesHistoryQuery(Guid GameId, int Skip, int Take) : IRequest<Result<FilteredMovesHistoryResponse>>;

public class GetMovesHistoryQueryHandler : IRequestHandler<GetMovesHistoryQuery, Result<FilteredMovesHistoryResponse>>
{
    private readonly IGameRepository _gameRepo;

    public GetMovesHistoryQueryHandler(IGameRepository gameRepo)
    {
        _gameRepo = gameRepo;
    }

    public async Task<Result<FilteredMovesHistoryResponse>> Handle(GetMovesHistoryQuery query, CancellationToken ct)
    {
        var result = await _gameRepo.GetMovesFilteredByAsync(query.Skip, query.Take, ct);
        return result;
    }
}