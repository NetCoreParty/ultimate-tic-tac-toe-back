using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using UltimateTicTacToe.Core.Features.Rooms;

namespace UltimateTicTacToe.Storage.Services;

public class MongoRoomMetricsStore : IRoomMetricsStore
{
    internal const string CollectionName = "rooms_metrics";
    private const string MetricsId = "rooms";

    private readonly IMongoCollection<RoomsMetricsDoc> _metrics;

    public MongoRoomMetricsStore(IMongoDatabase db)
    {
        _metrics = db.GetCollection<RoomsMetricsDoc>(CollectionName);
    }

    public async Task IncrementRoomsCreatedAsync(RoomType type, CancellationToken ct)
    {
        var filter = Builders<RoomsMetricsDoc>.Filter.Eq(x => x.Id, MetricsId);

        UpdateDefinition<RoomsMetricsDoc> update = type switch
        {
            RoomType.Regular => Builders<RoomsMetricsDoc>.Update.Inc(x => x.RegularCreated, 1),
            RoomType.Private => Builders<RoomsMetricsDoc>.Update.Inc(x => x.PrivateCreated, 1),
            _ => Builders<RoomsMetricsDoc>.Update.Inc(x => x.RegularCreated, 0),
        };

        await _metrics.UpdateOneAsync(filter, update, new UpdateOptions { IsUpsert = true }, ct);
    }

    public async Task<(long RegularCreated, long PrivateCreated)> GetRoomsCreatedCountersAsync(CancellationToken ct)
    {
        var doc = await _metrics.Find(x => x.Id == MetricsId).FirstOrDefaultAsync(ct);
        return doc == null ? (0, 0) : (doc.RegularCreated, doc.PrivateCreated);
    }

    internal class RoomsMetricsDoc
    {
        [BsonId]
        public string Id { get; set; } = MetricsId;

        public long RegularCreated { get; set; }

        public long PrivateCreated { get; set; }
    }
}

