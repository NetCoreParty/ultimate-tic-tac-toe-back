using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using UltimateTicTacToe.Core.Configuration;
using UltimateTicTacToe.Core.Domain.Aggregate;
using UltimateTicTacToe.Core.Features.GameSaving;
using UltimateTicTacToe.Core.Projections;

namespace UltimateTicTacToe.Core.Services;

public interface IGameRepository
{
    Task<Result<StartGameResponse>> TryStartGameAsync(CancellationToken ct = default);

    /// <summary>
    /// Create a new game for two known players (used by rooms/matchmaking).
    /// </summary>
    Task<Result<StartGameResponse>> TryStartGameForPlayersAsync(Guid playerXId, Guid playerOId, Guid? gameId = null, CancellationToken ct = default);

    Task<Result<bool>> TryMakeMoveAsync(PlayerMoveRequest move, CancellationToken ct = default);

    Task<Result<bool>> TryClearFinishedGamesAsync(CancellationToken ct = default);
    Task<Result<FilteredMovesHistoryResponse>> GetMovesFilteredByAsync(Guid gameId, int skip, int take, CancellationToken ct);

    int GamesNow { get; }
}

public class InMemoryGameRepository : IGameRepository
{
    private readonly IEventStore _eventStore;
    private readonly IStateSnapshotStore _snapshotStore;
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
        IStateSnapshotStore snapshotStore,
        ILogger<InMemoryGameRepository> logger,
        IOptions<GameplaySettings> gameplaySettings
        )
    {
        _eventStore = eventStore;
        _snapshotStore = snapshotStore;
        _logger = logger;
        _gameplaySettings = gameplaySettings.Value;
    }

    public int GamesNow => _games.Count;

    public async Task<Result<StartGameResponse>> TryStartGameAsync(CancellationToken ct = default)
        => await TryCreateGameAsync(ct);

    public async Task<Result<StartGameResponse>> TryStartGameForPlayersAsync(Guid playerXId, Guid playerOId, Guid? gameId = null, CancellationToken ct = default)
        => await TryCreateGameAsync(playerXId, playerOId, gameId, ct);

    private async Task<Result<StartGameResponse>> TryCreateGameAsync(CancellationToken ct)
    {
        // Legacy endpoint: creates random players (kept for now).
        var gameId = Guid.NewGuid();
        var onePlayerId = Guid.NewGuid();
        var anotherPlayerId = Guid.NewGuid();
        return await TryCreateGameAsync(onePlayerId, anotherPlayerId, gameId, ct);
    }

    private async Task<Result<StartGameResponse>> TryCreateGameAsync(Guid playerXId, Guid playerOId, Guid? gameId, CancellationToken ct)
    {
        var acquired = await _semaphore.WaitAsync(_maxSemaphoreTimeoutMs, ct);
        if (!acquired)
            return Result<StartGameResponse>.Failure(503, "Server is busy. Please retry.");

        try
        {
            // Capacity gate (backpressure) to avoid admitting new games too close to the hard cap.
            // NOTE: With rooms/matchmaking, apply the same logic to queue join + private room create/join.
            var max = _gameplaySettings.MaxActiveGames;
            var pct = _gameplaySettings.BackpressureThresholdPercent <= 0 ? 100 : _gameplaySettings.BackpressureThresholdPercent;
            var threshold = (int)Math.Ceiling(max * (pct / 100.0));

            if (_games.Count >= _gameplaySettings.MaxActiveGames)
            {
                return Result<StartGameResponse>.Failure(429, "Please try later. Too many parallel games in memory.");
            }

            if (_games.Count >= threshold)
            {
                return Result<StartGameResponse>.Failure(
                    429,
                    $"Server is near capacity. Please retry. (active={_games.Count}, threshold={threshold}, cap={max})"
                );
            }

            var resolvedGameId = gameId ?? Guid.NewGuid();

            var gameRoot = GameRoot.CreateNew(resolvedGameId, playerXId, playerOId);

            if (!_games.TryAdd(gameRoot.GameId, gameRoot))
            {
                return Result<StartGameResponse>.Failure(400, $"Game with ID {gameRoot.GameId} already exists. Please try again.");
            }

            try
            {
                // Persist the creation event so the game can be rehydrated after restart.
                await _eventStore.AppendEventsAsync(gameRoot.GameId, gameRoot.UncommittedChanges, ct);
                await _snapshotStore.TryCreateSnapshotAsync(gameRoot);
                GameRoot.ClearUncomittedEvents(gameRoot);
            }
            catch (Exception ex)
            {
                _games.TryRemove(gameRoot.GameId, out _);
                _logger.LogError($"{nameof(InMemoryGameRepository)}:{nameof(TryCreateGameAsync)}(): Failed to persist GameCreatedEvent: {ex.GetType().Name}: {ex.Message}");
                return Result<StartGameResponse>.Failure(500, "Failed to persist game creation event.");
            }

            return Result<StartGameResponse>.Success(
                new StartGameResponse(gameRoot.GameId, gameRoot.PlayerXId, gameRoot.PlayerOId, gameRoot.Status)
            );
        }
        finally
        {
            if (acquired)
                _semaphore.Release();
        }
    }

    public async Task<Result<bool>> TryMakeMoveAsync(PlayerMoveRequest move, CancellationToken ct = default)
    {
        var acquired = await _semaphore.WaitAsync(_maxSemaphoreTimeoutMs, ct);
        if (!acquired)
            return Result<bool>.Failure(503, "Server is busy. Please retry.");

        try
        {
            // Load from memory or rehydrate from the event store (restart-safe).
            if (!_games.TryGetValue(move.GameId, out var gameRoot))
            {
                gameRoot = await TryRehydrateGameAsync(move.GameId, ct);
                if (gameRoot == null)
                {
                    _logger.LogError($"{nameof(InMemoryGameRepository)}:{nameof(TryMakeMoveAsync)}(): Game with ID {move.GameId} not found.");
                    return Result<bool>.Failure(404, $"Game with ID {move.GameId} not found.");
                }

                _games[move.GameId] = gameRoot;
            }

            if (gameRoot != null)
            {
                gameRoot.PlayMove(
                    move.PlayerId,
                    move.MiniBoardRowId,
                    move.MiniBoardColId,
                    move.CellRowId,
                    move.CellColId
                );

                var newEvents = gameRoot.UncommittedChanges.ToList();

                try
                {
                    await _eventStore.AppendEventsAsync(gameRoot.GameId, newEvents, ct);
                    await _snapshotStore.TryCreateSnapshotAsync(gameRoot);
                    GameRoot.ClearUncomittedEvents(gameRoot);
                }
                catch (Exception ex)
                {
                    // Best-effort: revert in-memory state to persisted state (discard the failed move)
                    _logger.LogError($"{nameof(InMemoryGameRepository)}:{nameof(TryMakeMoveAsync)}(): Failed to persist move events: {ex.GetType().Name}: {ex.Message}");
                    var restored = await TryRehydrateGameAsync(move.GameId, ct);
                    if (restored != null)
                        _games[move.GameId] = restored;
                    return Result<bool>.Failure(500, "Failed to persist move. Please retry.");
                }

                if (newEvents.Count >= _gameplaySettings.EventsUntilSnapshot)
                {
                    _logger.LogWarning($"{nameof(InMemoryGameRepository)}:{nameof(TryMakeMoveAsync)}(): " +
                        $"GameRoot: {gameRoot.GameId} produced {_gameplaySettings.EventsUntilSnapshot} or more new events!" +
                        $"Maybe its time to take a Snapshot.");
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

            return Result<bool>.Failure(404, $"Game with ID {move.GameId} not found.");
        }
        catch (Exception ex)
        {
            _logger.LogError($"{nameof(InMemoryGameRepository)}:{nameof(TryMakeMoveAsync)}(): Exception was caught: {ex.GetType().Name}: {ex.Message}");
            return Result<bool>.Failure(500, $"Error while making move: {ex.Message}");
        }
        finally
        {
            if (acquired)
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

    public async Task<Result<FilteredMovesHistoryResponse>> GetMovesFilteredByAsync(Guid gameId, int skip, int take, CancellationToken ct)
    {
        if (skip < 0) skip = 0;
        if (take <= 0) take = 10;
        if (take > 100) take = 100;

        var allEvents = await _eventStore.GetAllEventsAsync(gameId, ct) ?? new List<Domain.Events.IDomainEvent>();
        if (allEvents.Count == 0)
            return Result<FilteredMovesHistoryResponse>.Failure(404, $"Game with ID {gameId} not found.");

        var cellEvents = allEvents
            .OfType<Domain.Events.CellMarkedEvent>()
            .OrderBy(e => e.Version)
            .ToList();

        var allMoves = cellEvents
            .Select((e, idx) => new PlayerHistoricalMove(
                PlayerId: e.PlayerId,
                MoveId: idx + 1,
                MiniBoardRowId: e.MiniBoardRowId,
                MiniBoardColId: e.MiniBoardColId,
                CellRowId: e.CellRowId,
                CellColId: e.CellColId
            ))
            .ToList();

        var page = allMoves.Skip(skip).Take(take).ToList();

        return Result<FilteredMovesHistoryResponse>.Success(new FilteredMovesHistoryResponse(gameId, page));
    }

    private async Task<GameRoot?> TryRehydrateGameAsync(Guid gameId, CancellationToken ct)
    {
        try
        {
            var game = await _snapshotStore.TryLoadGameAsync(gameId, _eventStore, ct);
            return game;
        }
        catch (Exception ex)
        {
            _logger.LogError($"{nameof(InMemoryGameRepository)}:{nameof(TryRehydrateGameAsync)}(): Failed to rehydrate {gameId}: {ex.GetType().Name}: {ex.Message}");
            return null;
        }
    }
}