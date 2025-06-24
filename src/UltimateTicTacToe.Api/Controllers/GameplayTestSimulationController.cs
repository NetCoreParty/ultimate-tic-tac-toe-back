using Microsoft.AspNetCore.Mvc;
using UltimateTicTacToe.Core.Domain.Aggregate;
using UltimateTicTacToe.Core.Services;

namespace UltimateTicTacToe.API.Controllers;

[Route("api/gameplay-test-simulation")]
public class GamePlayTestSimulationController : ControllerBase
{
    private readonly IEventStore _eventStore;

    public GamePlayTestSimulationController(IEventStore eventStore)
    {
        _eventStore = eventStore;
    }

    [HttpPost("play-and-clear-events")]
    public async Task<IActionResult> SimulateGame(CancellationToken ct = default)
    {
        var gameId = Guid.NewGuid();
        var onePlayerId = Guid.NewGuid();
        var anotherPlayerId = Guid.NewGuid();

        var gameRoot = GameRoot.CreateNew(gameId, onePlayerId, anotherPlayerId);
        
        gameRoot.PlayMove(onePlayerId, 0, 0, 0, 0);
        gameRoot.PlayMove(anotherPlayerId, 0, 0, 0, 1);

        await _eventStore.AppendEventsAsync(gameId, gameRoot.UncommittedChanges, ct);

        var lastEvents = await _eventStore.GetEventsAfterVersionAsync(gameId, gameRoot.Version - 2, ct);

        // Clear uncommitted events after saving to the event store
        GameRoot.ClearUncomittedEvents(gameRoot);
        
        await _eventStore.DeleteEventsByAsync(gameId, ct);

        return Ok(new
        {
            GameId = gameId,
            EventsPlayedSince = gameRoot.UncommittedChanges.Count // 3
        });
    }
}