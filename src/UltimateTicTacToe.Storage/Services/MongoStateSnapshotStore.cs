using System.Text.Json;
using Microsoft.Extensions.Options;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using UltimateTicTacToe.Core.Configuration;
using UltimateTicTacToe.Core.Domain.Aggregate;
using UltimateTicTacToe.Core.Domain.Events;
using UltimateTicTacToe.Core.Features.GameSave;
using UltimateTicTacToe.Core.Features.GameSave.Entities;
using UltimateTicTacToe.Core.Features.GameSaving;
using UltimateTicTacToe.Core.Services;

namespace UltimateTicTacToe.Storage.Services;

public class MongoStateSnapshotStore : IStateSnapshotStore
{
    public const string CollectionName = "game_snapshots";

    private readonly IMongoCollection<SnapshotDoc> _snapshots;
    private readonly int _eventsUntilSnapshot;

    public MongoStateSnapshotStore(IMongoDatabase db, IOptions<GameplaySettings> gameplaySettings)
    {
        _snapshots = db.GetCollection<SnapshotDoc>(CollectionName);
        _eventsUntilSnapshot = gameplaySettings.Value.EventsUntilSnapshot > 0 ? gameplaySettings.Value.EventsUntilSnapshot : 20;
    }

    public async Task<int?> TryGetLatestSnapshotVersionAsync(Guid gameId)
    {
        var doc = await _snapshots
            .Find(s => s.GameId == gameId)
            .SortByDescending(s => s.Version)
            .Limit(1)
            .FirstOrDefaultAsync();

        return doc?.Version;
    }

    public async Task<int?> TryCreateSnapshotAsync(GameRoot gameRoot)
    {
        var currentVersion = gameRoot.Version;
        var latestSnapshotVersion = await TryGetLatestSnapshotVersionAsync(gameRoot.GameId);
        var versionSinceLast = currentVersion - (latestSnapshotVersion ?? 0);
        SnapshotCause? cause = null;

        var eventsDelta = gameRoot.UncommittedChanges;

        if (eventsDelta.Any(e => e is FullGameWonEvent))
            cause = SnapshotCause.GameWon;
        else if (eventsDelta.Any(e => e is GameDrawnEvent))
            cause = SnapshotCause.GameWon; // terminal
        else if (eventsDelta.Any(e => e is MiniBoardWonEvent))
            cause = SnapshotCause.MiniBoardWon;
        else if (versionSinceLast >= _eventsUntilSnapshot)
            cause = SnapshotCause.PeriodicThresholdReached;

        if (cause == null)
            return null;

        var projection = gameRoot.ToSnapshot();
        var json = JsonSerializer.Serialize(projection);

        var doc = new SnapshotDoc
        {
            Id = Guid.NewGuid(),
            GameId = gameRoot.GameId,
            Version = currentVersion,
            StateJson = json,
            CreatedAtUtc = DateTime.UtcNow,
            Cause = cause.Value
        };

        await _snapshots.InsertOneAsync(doc);
        return currentVersion;
    }

    public async Task<GameRoot?> TryLoadGameAsync(Guid gameId, IEventStore eventStore, CancellationToken ct = default)
    {
        var latest = await _snapshots
            .Find(s => s.GameId == gameId)
            .SortByDescending(s => s.Version)
            .Limit(1)
            .FirstOrDefaultAsync(ct);

        if (latest != null)
        {
            var snapshotProjection = JsonSerializer.Deserialize<GameRootSnapshotProjection>(latest.StateJson)!;
            var remainingEvents = await eventStore.GetEventsAfterVersionAsync(gameId, latest.Version, ct);
            return snapshotProjection.ToGameRoot(remainingEvents);
        }

        var allEvents = await eventStore.GetAllEventsAsync(gameId, ct);
        if (allEvents == null || allEvents.Count == 0)
            return null;

        return GameRoot.Rehydrate(allEvents, null);
    }

    public class SnapshotDoc
    {
        [BsonId]
        public Guid Id { get; set; }

        public Guid GameId { get; set; }

        public int Version { get; set; }

        public string StateJson { get; set; } = string.Empty;

        public DateTime CreatedAtUtc { get; set; }

        public SnapshotCause Cause { get; set; }
    }
}

