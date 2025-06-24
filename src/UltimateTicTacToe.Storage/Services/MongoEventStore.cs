using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using UltimateTicTacToe.Core.Configuration;
using UltimateTicTacToe.Core.Domain.Events;
using UltimateTicTacToe.Core.Services;

namespace UltimateTicTacToe.Storage.Services;

public class MongoEventStore : IEventStore
{
    private readonly IMongoCollection<StoredEvent> _collection;

    public MongoEventStore(IMongoDatabase database, IOptions<EventStoreSettings> settings)
    {
        _collection = database.GetCollection<StoredEvent>(settings.Value.EventsCollectionName);
    }    

    public async Task AppendEventsAsync(Guid gameAggregateId, IEnumerable<IDomainEvent> events, CancellationToken ct = default)
    {
        var storedEvents = events.Select(e => new StoredEvent
        {
            Id = ObjectId.GenerateNewId(),
            AggregateId = gameAggregateId,
            EventVersion = e.Version,
            Type = e.GetType().FullName!,
            Data = e,
            OccurredOn = e.OccurredOn
        });

        await _collection.InsertManyAsync(storedEvents);
    }

    public async Task<List<IDomainEvent>> GetAllEventsAsync(Guid gameAggregateId, CancellationToken ct = default)
    {
        var storedEvents = await _collection
            .Find(e => e.AggregateId == gameAggregateId)
            .ToListAsync();

        return storedEvents
            .Select(e => e.Data)
            .ToList();
    }

    public async Task<List<IDomainEvent>> GetEventsAfterVersionAsync(Guid gameAggregateId, int version, CancellationToken ct = default)
    {
        var storedEvents = await _collection
            .Find(e => e.AggregateId == gameAggregateId && e.EventVersion > version)
            .SortBy(e => e.OccurredOn)
            .ToListAsync();

        return storedEvents
            .Select(e => e.Data)
            .ToList();
    }

    public async Task DeleteEventsByAsync(Guid gameAggregateId, CancellationToken ct = default)
    {
        await _collection.DeleteManyAsync(e => e.AggregateId == gameAggregateId);
    }
}

public class StoredEvent
{
    [BsonId]
    public ObjectId Id { get; set; }

    public int EventVersion { get; set; }

    public Guid AggregateId { get; set; } = default!;

    public string Type { get; set; } = default!;

    [BsonElement("Data")]
    public IDomainEvent Data { get; set; } = default!;

    public DateTime OccurredOn { get; set; }
}