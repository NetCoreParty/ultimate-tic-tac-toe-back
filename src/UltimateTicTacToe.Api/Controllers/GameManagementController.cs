using MediatR;
using Microsoft.AspNetCore.Mvc;
using UltimateTicTacToe.Core.Features.GameManagement;

namespace UltimateTicTacToe.API.Controllers;

[ApiController]
[Route("api/game-management")]
public class GameManagementController : ControllerBase
{
    private readonly IMediator _mediator;

    public GameManagementController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpDelete("clear")]
    public async Task<IActionResult> ClearFinishedGames(CancellationToken ct = default)
    {
        var clearFinishedGamesResult = await _mediator.Send(new ClearFinishedGamesCommand(), ct);
        return clearFinishedGamesResult.ToActionResult();
    }

    [HttpGet("{gameId}/moves-history")]
    public async Task<IActionResult> GetMovesHistory(Guid gameId, int skip = 0, int take = 10, CancellationToken ct = default)
    {
        var movesHistoryResult = await _mediator.Send(new GetMovesHistoryQuery(gameId, skip, take), ct);
        return movesHistoryResult.ToActionResult();
    }
}