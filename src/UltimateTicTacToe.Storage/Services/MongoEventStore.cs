using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Text.Json;
using UltimateTicTacToe.Core.Configuration;
using UltimateTicTacToe.Core.Features.Game.Domain.Events;
using UltimateTicTacToe.Core.Services;

namespace UltimateTicTacToe.Storage.Services;

public class MongoEventStore : IEventStore
{
    private readonly IMongoCollection<BsonDocument> _collection;

    public MongoEventStore(IOptions<EventStoreSettings> settings)
    {
        var client = new MongoClient(settings.Value.ConnectionString);
        var database = client.GetDatabase(settings.Value.DatabaseName);
        _collection = database.GetCollection<BsonDocument>(settings.Value.EventsCollectionName);
    }

    public async Task AppendEventsAsync(Guid gameAggregateId, IEnumerable<IDomainEvent> events)
    {
        var docs = events.Select(e => new BsonDocument
        {
            { "AggregateId", gameAggregateId.ToString() },
            { "Type", e.GetType().Name },
            { "Data", BsonDocument.Parse(JsonSerializer.Serialize(e)) },
            { "OccurredOn", e.OccurredOn }
        });

        await _collection.InsertManyAsync(docs);
    }

    public async Task<List<IDomainEvent>> GetAllEvents(Guid gameAggregateId)
    {
        var filter = Builders<BsonDocument>.Filter.Eq("AggregateId", gameAggregateId);
        var docs = await _collection.Find(filter).ToListAsync();

        return docs.Select(DeserializeEvent).ToList();
    }

    public async Task<List<IDomainEvent>> GetEventsAfterVersion(Guid gameAggregateId, int version)
    {
        var filter = Builders<BsonDocument>.Filter.And(
            Builders<BsonDocument>.Filter.Eq("AggregateId", gameAggregateId),
            Builders<BsonDocument>.Filter.Gt("Version", version)
        );

        var docs = await _collection.Find(filter).ToListAsync();

        return docs.Select(DeserializeEvent).ToList();
    }

    private IDomainEvent DeserializeEvent(BsonDocument doc)
    {
        var typeName = doc["Type"].AsString;
        var json = doc["Data"].ToJson();

        var type = Type.GetType($"{nameof(UltimateTicTacToe.Core.Features.Game.Domain.Events)}.{typeName}");
        return (IDomainEvent)JsonSerializer.Deserialize(json, type!)!;
    }
}