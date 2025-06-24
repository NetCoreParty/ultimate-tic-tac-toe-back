using System.Text.Json;
using UltimateTicTacToe.Core.Domain.Aggregate;
using UltimateTicTacToe.Core.Domain.Events;
using UltimateTicTacToe.Core.Features.GameSave;
using UltimateTicTacToe.Core.Features.GameSave.Entities;
using UltimateTicTacToe.Core.Services;

namespace UltimateTicTacToe.Core.Features.GameSaving;

public interface IStateSnapshotStore
{
    Task<int?> TryGetLatestSnapshotVersionAsync(Guid gameId);

    Task<int?> TryCreateSnapshotAsync(GameRoot gameRoot);

    Task<GameRoot?> TryLoadGameAsync(Guid gameId, IEventStore eventStore);
}

public class StateSnapshotStore : IStateSnapshotStore
{
    private readonly Dictionary<Guid, StoredSnapshot> _inMemorySnapshots = new();

    private const int MaxEventsBeforeSnapshot = 20;

    public Task<int?> TryGetLatestSnapshotVersionAsync(Guid gameId)
    {
        if (_inMemorySnapshots.TryGetValue(gameId, out var snapshot))
            return Task.FromResult<int?>(snapshot.Version);

        return Task.FromResult<int?>(null);
    }

    public async Task<int?> TryCreateSnapshotAsync(GameRoot gameRoot)
    {
        var currentVersion = gameRoot.Version;
        var latestSnapshot = await TryGetLatestSnapshotVersionAsync(gameRoot.GameId);
        var versionSinceLast = currentVersion - (latestSnapshot ?? default);
        SnapshotCause? cause = null;
        var eventsDelta = gameRoot.UncommittedChanges;

        if (eventsDelta.Any(e => e is FullGameWonEvent))
            cause = SnapshotCause.GameWon;

        else if (eventsDelta.Any(e => e is MiniBoardWonEvent))
            cause = SnapshotCause.MiniBoardWon;

        else if (versionSinceLast >= MaxEventsBeforeSnapshot)
            cause = SnapshotCause.PeriodicThresholdReached;

        if (cause == null)
            return null;

        var snapshotProjection = gameRoot.ToSnapshot();
        var currentStateJson = JsonSerializer.Serialize(snapshotProjection);

        var newSnapshot = new StoredSnapshot
        {
            GameId = gameRoot.GameId,
            Version = currentVersion,
            StateJson = currentStateJson,
            Cause = cause.Value
        };

        _inMemorySnapshots[gameRoot.GameId] = newSnapshot;

        return currentVersion;
    }

    /// <summary>
    /// Rehydrates the game state from the snapshot and any remaining events.
    /// </summary>
    /// <returns></returns>
    public async Task<GameRoot?> TryLoadGameAsync(Guid gameId, IEventStore eventStore)
    {
        if (_inMemorySnapshots.TryGetValue(gameId, out var snapshot))
        {
            var snapshotProjection = JsonSerializer.Deserialize<GameRootSnapshotProjection>(snapshot.StateJson)!;
            var remainingEvents = await eventStore.GetEventsAfterVersionAsync(gameId, snapshot.Version);
            var gameRoot = snapshotProjection.ToGameRoot(remainingEvents);

            return gameRoot;
        }

        // Now we gonna replay all events from the beginning
        var allEvents = await eventStore.GetAllEventsAsync(gameId);
        var replayedGameRoot = GameRoot.Rehydrate(allEvents, null);

        return replayedGameRoot;
    }
}