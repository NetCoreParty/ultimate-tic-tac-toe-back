using Microsoft.AspNetCore.Mvc;
using UltimateTicTacToe.Core.Features.Game.Domain.Aggregate;
using UltimateTicTacToe.Core.Services;

namespace UltimateTicTacToe.API.Controllers;

[Route("api/[controller]")]
public class GameplaySimulationController : ControllerBase
{
    private readonly IEventStore _eventStore;

    public GameplaySimulationController(IEventStore eventStore)
    {
        _eventStore = eventStore;
    }

    [HttpPost("clear-and-save-events")]
    public async Task<IActionResult> SimulateGame()
    {
        var gameId = Guid.NewGuid();
        var onePlayerId = Guid.NewGuid();
        var anotherPlayerId = Guid.NewGuid();

        var gameRoot = GameRoot.CreateNew(gameId, onePlayerId, anotherPlayerId);
        
        gameRoot.PlayMove(onePlayerId, 0, 0, 0, 0);
        gameRoot.PlayMove(anotherPlayerId, 0, 0, 0, 1);

        await _eventStore.AppendEventsAsync(gameId, gameRoot.UncommittedChanges);

        var lastEvents = await _eventStore.GetEventsAfterVersionAsync(gameId, gameRoot.Version - 2);

        // Clear uncommitted events after saving to the event store
        GameRoot.ClearUncomittedEvents(gameRoot);
        
        await _eventStore.DeleteEventsByAsync(gameId);

        return Ok(new
        {
            GameId = gameId,
            EventsPlayedSince = gameRoot.UncommittedChanges.Count // 3
        });
    }
}