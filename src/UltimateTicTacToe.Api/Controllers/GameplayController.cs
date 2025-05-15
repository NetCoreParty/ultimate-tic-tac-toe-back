using Microsoft.AspNetCore.Mvc;
using UltimateTicTacToe.Core.Features.Gameplay;

namespace UltimateTicTacToe.API.Controllers;

[ApiController]
[Route("api/game")]
public class GameplayController : ControllerBase
{
    private readonly IGameRepository _gameRepo;

    public GameplayController(IGameRepository gameService)
    {
        _gameRepo = gameService;
    }

    /*
        Gameplay HTTP Endpoints

        Method		URL			                Purpose

        POST		api/game/start		        Start a new game
        POST		api/game/move   		    Make a move in the game        
        DELETE		api/game/clear		        Delete all the finished games (WON or DRAW) from memory
        GET		    api/game/metrics/games-now	Get all in-memory games count
    */

    [HttpPost("start")]
    public async Task<IActionResult> StartGame(CancellationToken ct = default)
    {
        var result = await _gameRepo.TryStartGameAsync(ct);
        return result.ToActionResult();
    }

    [HttpPost("move")]
    public async Task<IActionResult> MakeMove([FromBody] PlayerMoveRequest makeMoveRequest, CancellationToken ct = default)
    {
        var result = await _gameRepo.TryMakeMoveAsync(makeMoveRequest, ct);
        return result.ToActionResult();
    }

    [HttpDelete("clear")]
    public async Task<IActionResult> ClearFinishedGames(CancellationToken ct)
    {
        var result = await _gameRepo.TryClearFinishedGames(ct);
        return result.ToActionResult();
    }

    [HttpGet("metrics/games-now")]
    public IActionResult GetGamesNow() => Ok(new { _gameRepo.GamesNow });
}

public static class ResultExtensions
{
    public static IActionResult ToActionResult<T>(this Result<T> result)
    {
        return result.IsSuccess
            ? new OkObjectResult(result.Value) { StatusCode = result.Code }
            : new ObjectResult(new { error = result.Error }) { StatusCode = result.Code };
    }
}