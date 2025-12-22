using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using UltimateTicTacToe.Core.Features.Rooms;

namespace UltimateTicTacToe.Storage.Services;

public class MongoRoomStore : IRoomStore
{
    internal const string CollectionName = "rooms";
    private readonly IMongoCollection<RoomDoc> _rooms;

    public MongoRoomStore(IMongoDatabase db)
    {
        _rooms = db.GetCollection<RoomDoc>(CollectionName);
    }

    public async Task<int> CountActiveRoomsAsync(RoomType type, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var count = await _rooms.CountDocumentsAsync(
            r => r.Type == type && r.Status == RoomStatus.Waiting && r.ExpiresAtUtc > now,
            cancellationToken: ct
        );
        return (int)count;
    }

    public async Task<RoomDto> CreatePrivateRoomAsync(Guid userId, string joinCode, DateTime nowUtc, DateTime expiresAtUtc, CancellationToken ct)
    {
        var doc = new RoomDoc
        {
            RoomId = Guid.NewGuid(),
            Type = RoomType.Private,
            Status = RoomStatus.Waiting,
            JoinCode = joinCode,
            CreatedAtUtc = nowUtc,
            ExpiresAtUtc = expiresAtUtc,
            Players = new List<RoomPlayerDoc> { new() { UserId = userId, JoinedAtUtc = nowUtc } }
        };

        await _rooms.InsertOneAsync(doc, cancellationToken: ct);
        return doc.ToDto();
    }

    public async Task<RoomDto?> TryJoinPrivateRoomAsync(Guid userId, string joinCode, DateTime nowUtc, CancellationToken ct)
    {
        var filter = Builders<RoomDoc>.Filter.And(
            Builders<RoomDoc>.Filter.Eq(x => x.Type, RoomType.Private),
            Builders<RoomDoc>.Filter.Eq(x => x.Status, RoomStatus.Waiting),
            Builders<RoomDoc>.Filter.Eq(x => x.JoinCode, joinCode),
            Builders<RoomDoc>.Filter.Gt(x => x.ExpiresAtUtc, nowUtc),
            Builders<RoomDoc>.Filter.Size(x => x.Players, 1),
            Builders<RoomDoc>.Filter.Ne("Players.0.UserId", userId)
        );

        var update = Builders<RoomDoc>.Update
            .Push(x => x.Players, new RoomPlayerDoc { UserId = userId, JoinedAtUtc = nowUtc })
            .Set(x => x.Status, RoomStatus.Matched);

        var options = new FindOneAndUpdateOptions<RoomDoc>
        {
            ReturnDocument = ReturnDocument.After
        };

        var updated = await _rooms.FindOneAndUpdateAsync(filter, update, options, ct);
        return updated?.ToDto();
    }

    public async Task<RoomDto?> TryJoinWaitingRegularRoomAsync(Guid userId, DateTime nowUtc, DateTime expiresAtUtc, CancellationToken ct)
    {
        var filter = Builders<RoomDoc>.Filter.And(
            Builders<RoomDoc>.Filter.Eq(x => x.Type, RoomType.Regular),
            Builders<RoomDoc>.Filter.Eq(x => x.Status, RoomStatus.Waiting),
            Builders<RoomDoc>.Filter.Gt(x => x.ExpiresAtUtc, nowUtc),
            Builders<RoomDoc>.Filter.Size(x => x.Players, 1),
            Builders<RoomDoc>.Filter.Ne("Players.0.UserId", userId)
        );

        var update = Builders<RoomDoc>.Update
            .Push(x => x.Players, new RoomPlayerDoc { UserId = userId, JoinedAtUtc = nowUtc })
            .Set(x => x.Status, RoomStatus.Matched);

        var options = new FindOneAndUpdateOptions<RoomDoc>
        {
            ReturnDocument = ReturnDocument.After
        };

        var updated = await _rooms.FindOneAndUpdateAsync(filter, update, options, ct);
        return updated?.ToDto();
    }

    public async Task<RoomDto> CreateWaitingRegularRoomAsync(Guid userId, DateTime nowUtc, DateTime expiresAtUtc, CancellationToken ct)
    {
        var doc = new RoomDoc
        {
            RoomId = Guid.NewGuid(),
            Type = RoomType.Regular,
            Status = RoomStatus.Waiting,
            JoinCode = null,
            CreatedAtUtc = nowUtc,
            ExpiresAtUtc = expiresAtUtc,
            Players = new List<RoomPlayerDoc> { new() { UserId = userId, JoinedAtUtc = nowUtc } }
        };

        await _rooms.InsertOneAsync(doc, cancellationToken: ct);
        return doc.ToDto();
    }

    public async Task<bool> DeleteRoomAsync(Guid roomId, CancellationToken ct)
    {
        var res = await _rooms.DeleteOneAsync(r => r.RoomId == roomId, ct);
        return res.DeletedCount > 0;
    }

    public async Task<IReadOnlyList<RoomDto>> GetExpiredHalfFullWaitingRoomsAsync(DateTime nowUtc, int take, CancellationToken ct)
    {
        // half-full = exactly 1 player, waiting, expired.
        var filter = Builders<RoomDoc>.Filter.And(
            Builders<RoomDoc>.Filter.Eq(x => x.Status, RoomStatus.Waiting),
            Builders<RoomDoc>.Filter.Lte(x => x.ExpiresAtUtc, nowUtc),
            Builders<RoomDoc>.Filter.Size(x => x.Players, 1)
        );

        var docs = await _rooms.Find(filter).SortBy(x => x.ExpiresAtUtc).Limit(take).ToListAsync(ct);
        return docs.Select(d => d.ToDto()).ToList();
    }

    internal class RoomDoc
    {
        [BsonId]
        public Guid RoomId { get; set; }

        public RoomType Type { get; set; }

        public RoomStatus Status { get; set; }

        public string? JoinCode { get; set; }

        public DateTime CreatedAtUtc { get; set; }

        public DateTime ExpiresAtUtc { get; set; }

        public List<RoomPlayerDoc> Players { get; set; } = new();

        public RoomDto ToDto()
            => new(RoomId, Type, Status, JoinCode, CreatedAtUtc, ExpiresAtUtc, Players.Select(p => new RoomPlayer(p.UserId, p.JoinedAtUtc)).ToList());
    }

    internal class RoomPlayerDoc
    {
        public Guid UserId { get; set; }
        public DateTime JoinedAtUtc { get; set; }
    }
}

