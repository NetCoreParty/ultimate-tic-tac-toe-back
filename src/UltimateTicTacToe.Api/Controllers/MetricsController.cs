using MediatR;
using Microsoft.AspNetCore.Mvc;
using UltimateTicTacToe.API.Extensions;
using UltimateTicTacToe.Core.Features.Metrics;

namespace UltimateTicTacToe.API.Controllers;

[ApiController]
[Route("api/metrics")]
public class MetricsController : ControllerBase
{
    private readonly IMediator _mediator;

    public MetricsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("unfinished-games")]
    public async Task<IActionResult> GetUnfinishedGames(CancellationToken ct = default)
    {
        var gamesUnfinishedResult = await _mediator.Send(new GetUnfinishedGamesQuery(), ct);
        return gamesUnfinishedResult.ToActionResult();
    }
}