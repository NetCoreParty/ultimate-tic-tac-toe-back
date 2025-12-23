using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using UltimateTicTacToe.Core.Features.Rooms;

namespace UltimateTicTacToe.Storage.Services;

public class MongoMatchmakingTicketStore : IMatchmakingTicketStore
{
    internal const string CollectionName = "matchmaking_tickets";
    private readonly IMongoCollection<TicketDoc> _tickets;

    public MongoMatchmakingTicketStore(IMongoDatabase db)
    {
        _tickets = db.GetCollection<TicketDoc>(CollectionName);
    }

    public async Task<int> CountQueuedTicketsAsync(DateTime nowUtc, CancellationToken ct)
    {
        var count = await _tickets.CountDocumentsAsync(
            t => t.Status == MatchmakingTicketStatus.Queued && t.ExpiresAtUtc > nowUtc,
            cancellationToken: ct
        );
        return (int)count;
    }

    public async Task<MatchmakingTicketDto> CreateQueuedTicketAsync(Guid userId, DateTime nowUtc, DateTime expiresAtUtc, CancellationToken ct)
    {
        var doc = new TicketDoc
        {
            TicketId = Guid.NewGuid(),
            UserId = userId,
            Status = MatchmakingTicketStatus.Queued,
            CreatedAtUtc = nowUtc,
            ExpiresAtUtc = expiresAtUtc
        };

        await _tickets.InsertOneAsync(doc, cancellationToken: ct);
        return doc.ToDto();
    }

    public async Task<bool> TryMarkMatchedAsync(Guid ticketId, Guid matchedRoomId, Guid gameId, CancellationToken ct)
    {
        var filter = Builders<TicketDoc>.Filter.And(
            Builders<TicketDoc>.Filter.Eq(x => x.TicketId, ticketId),
            Builders<TicketDoc>.Filter.Eq(x => x.Status, MatchmakingTicketStatus.Queued)
        );

        var update = Builders<TicketDoc>.Update
            .Set(x => x.Status, MatchmakingTicketStatus.Matched)
            .Set(x => x.MatchedRoomId, matchedRoomId)
            .Set(x => x.GameId, gameId);

        var res = await _tickets.UpdateOneAsync(filter, update, cancellationToken: ct);
        return res.ModifiedCount == 1;
    }

    public async Task<bool> TryCancelAsync(Guid ticketId, Guid userId, CancellationToken ct)
    {
        var filter = Builders<TicketDoc>.Filter.And(
            Builders<TicketDoc>.Filter.Eq(x => x.TicketId, ticketId),
            Builders<TicketDoc>.Filter.Eq(x => x.UserId, userId),
            Builders<TicketDoc>.Filter.Eq(x => x.Status, MatchmakingTicketStatus.Queued)
        );

        var update = Builders<TicketDoc>.Update.Set(x => x.Status, MatchmakingTicketStatus.Cancelled);
        var res = await _tickets.UpdateOneAsync(filter, update, cancellationToken: ct);
        return res.ModifiedCount == 1;
    }

    public async Task<IReadOnlyList<MatchmakingTicketDto>> GetExpiredQueuedTicketsAsync(DateTime nowUtc, int take, CancellationToken ct)
    {
        var filter = Builders<TicketDoc>.Filter.And(
            Builders<TicketDoc>.Filter.Eq(x => x.Status, MatchmakingTicketStatus.Queued),
            Builders<TicketDoc>.Filter.Lte(x => x.ExpiresAtUtc, nowUtc)
        );

        var docs = await _tickets.Find(filter).SortBy(x => x.ExpiresAtUtc).Limit(take).ToListAsync(ct);
        return docs.Select(d => d.ToDto()).ToList();
    }

    public async Task<bool> TryMarkExpiredAsync(Guid ticketId, CancellationToken ct)
    {
        var filter = Builders<TicketDoc>.Filter.And(
            Builders<TicketDoc>.Filter.Eq(x => x.TicketId, ticketId),
            Builders<TicketDoc>.Filter.Eq(x => x.Status, MatchmakingTicketStatus.Queued)
        );

        var update = Builders<TicketDoc>.Update.Set(x => x.Status, MatchmakingTicketStatus.Expired);
        var res = await _tickets.UpdateOneAsync(filter, update, cancellationToken: ct);
        return res.ModifiedCount == 1;
    }

    public async Task<MatchmakingTicketDto?> GetActiveTicketForUserAsync(Guid userId, DateTime nowUtc, CancellationToken ct)
    {
        var filter = Builders<TicketDoc>.Filter.And(
            Builders<TicketDoc>.Filter.Eq(x => x.UserId, userId),
            Builders<TicketDoc>.Filter.Eq(x => x.Status, MatchmakingTicketStatus.Queued),
            Builders<TicketDoc>.Filter.Gt(x => x.ExpiresAtUtc, nowUtc)
        );

        var doc = await _tickets.Find(filter).SortByDescending(x => x.CreatedAtUtc).Limit(1).FirstOrDefaultAsync(ct);
        return doc?.ToDto();
    }

    internal class TicketDoc
    {
        [BsonId]
        public Guid TicketId { get; set; }

        public Guid UserId { get; set; }

        public MatchmakingTicketStatus Status { get; set; }

        public DateTime CreatedAtUtc { get; set; }

        public DateTime ExpiresAtUtc { get; set; }

        public Guid? MatchedRoomId { get; set; }

        public Guid? GameId { get; set; }

        public MatchmakingTicketDto ToDto()
            => new(TicketId, UserId, Status, CreatedAtUtc, ExpiresAtUtc, MatchedRoomId, GameId);
    }
}

