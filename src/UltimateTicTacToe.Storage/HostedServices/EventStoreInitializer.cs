using DnsClient.Internal;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using UltimateTicTacToe.Core.Configuration;
using UltimateTicTacToe.Core.Services;
using UltimateTicTacToe.Storage.Services;

namespace UltimateTicTacToe.Storage.HostedServices;

public class EventStoreInitializer : IHostedService
{
    private readonly IMongoCollection<StoredEvent> _collection;
    private readonly ILogger<EventStoreInitializer> _logger;

    public EventStoreInitializer(IOptions<EventStoreSettings> settings, ILogger<EventStoreInitializer> logger)
    {
        var client = new MongoClient(settings.Value.ConnectionString);
        var database = client.GetDatabase(settings.Value.DatabaseName);
        
        _collection = database.GetCollection<StoredEvent>(settings.Value.EventsCollectionName);
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Initializing MongoDB Event Store...");
        await EnsureIndexesAsync(cancellationToken);
        _logger.LogInformation("MongoDB Event Store initialized successfully.");
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public async Task ClearIndexesAsync()
    {
        await _collection.Indexes.DropAllAsync();
    }

    public async Task<List<MongoIndexInfo>> GetAppliedIndexesInfo()
    {
        var indexes = await _collection.Indexes.ListAsync();
        var rawIndexes = await indexes.ToListAsync();

        return rawIndexes.Select(index => new MongoIndexInfo
        {
            Name = index["name"].AsString,
            KeyMap = index["key"]
                .AsBsonDocument
                .Elements
                .ToDictionary(
                    e => e.Name,
                    e => e.Value.ToInt32()), // 1 for ascending, -1 for descending index

            // MongoDB marks _id_ index as unique implicitly, so we default it to true if not specified
            IsUnique = index.GetValue("unique", index["name"] == "_id_").ToBoolean()

        }).ToList();
    }

    private async Task EnsureIndexesAsync(CancellationToken ct)
    {
        /*
            Field(s)    		    Index Type  	Purpose
            AggregateId    		    Single    	    Filtering by aggregate
            AggregateId + Version  	Compound  	    Efficient versioned lookups
            OccurredOn    		    Optional  	    Time-based sorting or replays

            // Used in nearly all queries, including loading all events or appending new ones
            db.StoredEvents.createIndex({ AggregateId: 1 })

            // Compound Index: AggregateId + Version Best for GetEventsAfterVersionAsync() or any versioned lookups.
            db.StoredEvents.createIndex({ AggregateId: 1, Version: 1 })

            // Useful if you want to query events chronologically, e.g., for analytics, replays, or projections.
            db.StoredEvents.createIndex({ OccurredOn: 1 })
         */

        var indexes = new List<CreateIndexModel<StoredEvent>>
        {
            // Single-field index: AggregateId
            new CreateIndexModel<StoredEvent>(
                Builders<StoredEvent>.IndexKeys.Ascending(e => e.AggregateId),
                new CreateIndexOptions { Name = "idx_aggregate_id" }
            ),

            // Compound index: AggregateId + Version
            new CreateIndexModel<StoredEvent>(
                Builders<StoredEvent>.IndexKeys
                    .Ascending(e => e.AggregateId)
                    .Ascending(e => e.EventVersion),
                new CreateIndexOptions { Name = "idx_aggregate_id__event_version" }
            ),

            // Optional index: OccurredOn (for time-based queries)
            new CreateIndexModel<StoredEvent>(
                Builders<StoredEvent>.IndexKeys.Ascending(e => e.OccurredOn),
                new CreateIndexOptions { Name = "idx_occurred_on" }
            )
        };

        // Its safe to call this multiple times, Even if those indexes already exist
        await _collection.Indexes.CreateManyAsync(indexes, ct);
    }

    public async Task ClearDatabaseAsync()
    {
        await _collection.Database.DropCollectionAsync(_collection.CollectionNamespace.CollectionName);
    }
}