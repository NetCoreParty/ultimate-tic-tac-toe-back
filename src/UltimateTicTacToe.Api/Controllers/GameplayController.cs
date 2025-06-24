using MediatR;
using Microsoft.AspNetCore.Mvc;
using UltimateTicTacToe.API.Extensions;
using UltimateTicTacToe.Core.Features.GamePlay;
using UltimateTicTacToe.Core.Projections;

namespace UltimateTicTacToe.API.Controllers;

[ApiController]
[Route("api/game")]
public class GamePlayController : ControllerBase
{
    private readonly IMediator _mediator;
    
    public GamePlayController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("start")]
    public async Task<IActionResult> StartGame(CancellationToken ct = default)
    {
        var newGameResult = await _mediator.Send(new StartGameCommand(), ct);
        return newGameResult.ToActionResult();
    }

    [HttpPost("move")]
    public async Task<IActionResult> MakeMove([FromBody] PlayerMoveRequest makeMoveRequest, CancellationToken ct = default)
    {
        var madeMoveResult = await _mediator.Send(new MakeMoveCommand(makeMoveRequest), ct);
        return madeMoveResult.ToActionResult();
    }
}