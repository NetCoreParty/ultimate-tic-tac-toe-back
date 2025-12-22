using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using UltimateTicTacToe.Storage.HostedServices;
using UltimateTicTacToe.Storage.Tests.Integration.Infrastructure;

namespace UltimateTicTacToe.Storage.Tests.Integration.Services;

[Collection("MongoEventStoreTests")]
public class MongoRoomStoreIndexTests : IClassFixture<MongoDbFixture>, IAsyncLifetime
{
    private readonly IMongoDatabase _db;
    private readonly RoomsStoreInitializer _initializer;

    public MongoRoomStoreIndexTests(MongoDbFixture fixture)
    {
        _db = fixture.Database;
        var logger = new Microsoft.Extensions.Logging.Abstractions.NullLogger<RoomsStoreInitializer>();
        _initializer = new RoomsStoreInitializer(_db, logger);
    }

    public async Task InitializeAsync()
    {
        await _initializer.StartAsync(CancellationToken.None);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task RoomsStoreInitializer_CreatesExpectedIndexes()
    {
        var rooms = _db.GetCollection<dynamic>("rooms");
        var tickets = _db.GetCollection<dynamic>("matchmaking_tickets");

        var roomIndexesCursor = await rooms.Indexes.ListAsync();
        var roomIndexes = await roomIndexesCursor.ToListAsync();
        Assert.Contains(roomIndexes, i => i["name"] == "ttl_rooms_expires_at");
        Assert.Contains(roomIndexes, i => i["name"] == "idx_rooms_type_status");
        Assert.Contains(roomIndexes, i => i["name"] == "uidx_rooms_join_code");

        var ticketIndexesCursor = await tickets.Indexes.ListAsync();
        var ticketIndexes = await ticketIndexesCursor.ToListAsync();
        Assert.Contains(ticketIndexes, i => i["name"] == "ttl_tickets_expires_at");
        Assert.Contains(ticketIndexes, i => i["name"] == "idx_tickets_status");
    }
}

