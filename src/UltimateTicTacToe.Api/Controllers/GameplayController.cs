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

    /// <summary>
    /// Host (Player X) forfeits; guest (Player O) soft-leaves. Identity from X-User-Id.
    /// </summary>
    [HttpPost("leave")]
    public async Task<IActionResult> LeaveGame([FromBody] LeaveGameRequest request, CancellationToken ct = default)
    {
        var userIdResult = TryGetUserId();
        if (!userIdResult.IsSuccess)
            return userIdResult.ToActionResult();

        var result = await _mediator.Send(new LeaveGameCommand(request.GameId, userIdResult.Value), ct);
        return result.ToActionResult();
    }

    private UltimateTicTacToe.Core.Result<Guid> TryGetUserId()
    {
        if (!Request.Headers.TryGetValue("X-User-Id", out var values))
            return UltimateTicTacToe.Core.Result<Guid>.Failure(400, "Missing X-User-Id header.");

        var raw = values.FirstOrDefault();
        if (!Guid.TryParse(raw, out var userId))
            return UltimateTicTacToe.Core.Result<Guid>.Failure(400, "Invalid X-User-Id header. Expected GUID.");

        return UltimateTicTacToe.Core.Result<Guid>.Success(userId);
    }
}
