using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using UltimateTicTacToe.Core.Configuration;
using UltimateTicTacToe.Core.Domain.Aggregate;
using UltimateTicTacToe.Core.Projections;

namespace UltimateTicTacToe.Core.Services;

public interface IGameRepository
{
    Task<Result<StartGameResponse>> TryStartGameAsync(CancellationToken ct = default);

    Task<Result<bool>> TryMakeMoveAsync(PlayerMoveRequest move, CancellationToken ct = default);

    Task<Result<bool>> TryClearFinishedGamesAsync(CancellationToken ct = default);
    Task<Result<FilteredMovesHistoryResponse>> GetMovesFilteredByAsync(int skip, int take, CancellationToken ct);

    int GamesNow { get; }
}

public class InMemoryGameRepository : IGameRepository
{
    private readonly IEventStore _eventStore;
    private readonly ILogger<InMemoryGameRepository> _logger;
    private readonly GameplaySettings _gameplaySettings;
    private const int _maxSemaphoreTimeoutMs = 400;

    private readonly ConcurrentDictionary<Guid, GameRoot> _games = new();
    private readonly SemaphoreSlim _semaphore = new(initialCount: 1, maxCount: 1);

    private readonly HashSet<GameStatus> _inactiveGameStatuses = new()
    {
        GameStatus.DRAW,
        GameStatus.WON
    };

    public InMemoryGameRepository(
        IEventStore eventStore,
        ILogger<InMemoryGameRepository> logger,
        IOptions<GameplaySettings> gameplaySettings
        )
    {
        _eventStore = eventStore;
        _logger = logger;
        _gameplaySettings = gameplaySettings.Value;
    }

    public int GamesNow => _games.Count;

    public async Task<Result<StartGameResponse>> TryStartGameAsync(CancellationToken ct = default)
        => await TryCreateGameAsync(ct);

    private async Task<Result<StartGameResponse>> TryCreateGameAsync(CancellationToken ct)
    {
        await _semaphore.WaitAsync(_maxSemaphoreTimeoutMs, ct);

        try
        {
            if (_games.Count >= _gameplaySettings.MaxActiveGames)
            {
                return Result<StartGameResponse>.Failure(429, "Please try later. Too many parallel games in memory.");
            }

            // TODO: rewrite later, its just mock, player guids should be created outside
            var gameId = Guid.NewGuid();
            var onePlayerId = Guid.NewGuid();
            var anotherPlayerId = Guid.NewGuid();

            var gameRoot = GameRoot.CreateNew(gameId, onePlayerId, anotherPlayerId);

            if (!_games.TryAdd(gameRoot.GameId, gameRoot))
            {
                return Result<StartGameResponse>.Failure(400, $"Game with ID {gameRoot.GameId} already exists.");
            }

            return Result<StartGameResponse>.Success(
                new StartGameResponse(gameRoot.GameId, gameRoot.PlayerXId, gameRoot.PlayerOId, gameRoot.Status)
                );
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<Result<bool>> TryMakeMoveAsync(PlayerMoveRequest move, CancellationToken ct = default)
    {
        // TODO: implement loading game feature later
        //var gameRoot = await _snapshotService.TryLoadGameAsync(move.GameId, _eventStore);

        await _semaphore.WaitAsync(_maxSemaphoreTimeoutMs, ct);

        try
        {
            if (_games.TryGetValue(move.GameId, out var gameRoot))
            {
                gameRoot.PlayMove(
                    move.PlayerId,
                    move.MiniBoardRowId,
                    move.MiniBoardColId,
                    move.CellRowId,
                    move.CellColId
                );

                if (gameRoot.UncommittedChanges.Count >= _gameplaySettings.EventsUntilSnapshot)
                {
                    _logger.LogWarning($"{nameof(InMemoryGameRepository)}:{nameof(TryMakeMoveAsync)}(): " +
                        $"GameRoot: {gameRoot.GameId} has {_gameplaySettings.EventsUntilSnapshot} or more Uncommitted Events!" +
                        $"Maybe its time to take a Snapshot.");

                    //await _snapshotService.TryCreateSnapshotAsync(gameId, gameRoot.Board, newEvents, gameRoot.Version);
                    //GameRoot.ClearUncomittedEvents(gameRoot);
                }

                if (gameRoot.Status == GameStatus.DRAW || gameRoot.Status == GameStatus.WON)
                {
                    _logger.LogWarning($"{nameof(InMemoryGameRepository)}:{nameof(TryMakeMoveAsync)}(): " +
                        $"GameRoot: {gameRoot.GameId} is ready for Snapshot!");

                    //_games.TryRemove(gameRoot.GameId, out _);
                    //await _snapshotService.TryCreateSnapshotAsync(gameId, gameRoot.Board, newEvents, gameRoot.Version);
                }

                _logger.LogInformation($"{nameof(InMemoryGameRepository)}:{nameof(TryMakeMoveAsync)}(): A move: {move.ToString()} was made alright!");
                return Result<bool>.Success(true);
            }

            _logger.LogError($"{nameof(InMemoryGameRepository)}:{nameof(TryMakeMoveAsync)}(): Game with ID {move.GameId} not found.");
            return Result<bool>.Failure(404, $"Game with ID {move.GameId} not found.");
        }
        catch (Exception ex)
        {
            _logger.LogError($"{nameof(InMemoryGameRepository)}:{nameof(TryMakeMoveAsync)}(): Exception was caught: {ex.GetType().Name}: {ex.Message}");
            return Result<bool>.Failure(400, $"Error while making move: {ex.Message}");
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public Task<Result<bool>> TryClearFinishedGamesAsync(CancellationToken ct = default)
    {
        var finishedGameIDs = _games
            .Where(x => _inactiveGameStatuses.Contains(x.Value.Status))
            .Select(x => x.Key)
            .ToList();

        foreach (var gameId in finishedGameIDs)
        {
            _games.TryRemove(gameId, out _);

            // TODO: Do it later, because we have to think about analytics and game history first
            //await _eventStore.DeleteEventsByAsync(gameId, ct);
        }

        if (finishedGameIDs.Count > 0)
        {
            _logger.LogInformation($"{nameof(InMemoryGameRepository)}:{nameof(TryClearFinishedGamesAsync)}(): Removed {finishedGameIDs.Count} finished games from memory.");
        }

        return Task.FromResult(
            Result<bool>.Success(true)
            );
    }

    public Task<Result<FilteredMovesHistoryResponse>> GetMovesFilteredByAsync(int skip, int take, CancellationToken ct)
    {
        throw new NotImplementedException();
    }
}