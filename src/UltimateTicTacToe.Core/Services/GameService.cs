using UltimateTicTacToe.Core.Features.Game.Domain.Aggregate;
using UltimateTicTacToe.Core.Features.Game.Domain.Entities;
using UltimateTicTacToe.Core.Features.Game.Domain.Exceptions;

namespace UltimateTicTacToe.Core.Services;

public class GameService
{
    private readonly IEventStore _eventStore;
    private readonly StateSnapshotStore _snapshotService;

    public GameService(IEventStore eventStore, StateSnapshotStore snapshotService)
    {
        _eventStore = eventStore;
        _snapshotService = snapshotService;
    }

    public async Task MakeMoveAsync(Guid gameId, PlayerMove move)
    {
        var gameRoot = await _snapshotService.TryLoadGameAsync(gameId, _eventStore);

        if (gameRoot == null)
        {
            throw new GameNotInProgressException();
        }

        if (gameRoot.Status != GameStatus.IN_PROGRESS)
        {
            throw new GameNotFoundException();
        }

        if (gameRoot.UncommittedChanges.Any())
        {
            await _eventStore.AppendEventsAsync(gameId, gameRoot.UncommittedChanges);
        }

        if (gameRoot.Board.TryMakeMove(
            move.MiniBoardRowId,
            move.MiniBoardColId,
            move.CellRowId,
            move.CellColId,
            move.PlayerFigure)
            )
        {

        }

        //await _snapshotService.TryCreateSnapshotAsync(gameId, gameRoot.Board, newEvents, gameRoot.Version);
    }
}

public record PlayerMove(
    Guid PlayerId,
    PlayerFigure PlayerFigure,
    int MiniBoardRowId,
    int MiniBoardColId,
    int CellRowId,
    int CellColId
    );