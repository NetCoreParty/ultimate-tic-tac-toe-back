using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace UltimateTicTacToe.Storage.HostedServices;

public class RoomsStoreInitializer : IHostedService
{
    private readonly IMongoDatabase _db;
    private readonly ILogger<RoomsStoreInitializer> _logger;

    public RoomsStoreInitializer(IMongoDatabase db, ILogger<RoomsStoreInitializer> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Initializing MongoDB Rooms collections...");
        await EnsureRoomsIndexesAsync(cancellationToken);
        await EnsureTicketsIndexesAsync(cancellationToken);
        _logger.LogInformation("MongoDB Rooms collections initialized successfully.");
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private async Task EnsureRoomsIndexesAsync(CancellationToken ct)
    {
        var rooms = _db.GetCollection<dynamic>(Services.MongoRoomStore.CollectionName);

        // TTL on ExpiresAtUtc
        await rooms.Indexes.CreateOneAsync(
            new CreateIndexModel<dynamic>(
                Builders<dynamic>.IndexKeys.Ascending("ExpiresAtUtc"),
                new CreateIndexOptions { Name = "ttl_rooms_expires_at", ExpireAfter = TimeSpan.Zero }
            ),
            options: null,
            cancellationToken: ct
        );

        // Query support
        await rooms.Indexes.CreateOneAsync(
            new CreateIndexModel<dynamic>(
                Builders<dynamic>.IndexKeys.Ascending("Type").Ascending("Status"),
                new CreateIndexOptions { Name = "idx_rooms_type_status" }
            ),
            options: null,
            cancellationToken: ct
        );

        // Unique join code (private rooms)
        await rooms.Indexes.CreateOneAsync(
            new CreateIndexModel<dynamic>(
                Builders<dynamic>.IndexKeys.Ascending("JoinCode"),
                new CreateIndexOptions { Name = "uidx_rooms_join_code", Unique = true, Sparse = true }
            ),
            options: null,
            cancellationToken: ct
        );
    }

    private async Task EnsureTicketsIndexesAsync(CancellationToken ct)
    {
        var tickets = _db.GetCollection<dynamic>(Services.MongoMatchmakingTicketStore.CollectionName);

        // TTL on ExpiresAtUtc
        await tickets.Indexes.CreateOneAsync(
            new CreateIndexModel<dynamic>(
                Builders<dynamic>.IndexKeys.Ascending("ExpiresAtUtc"),
                new CreateIndexOptions { Name = "ttl_tickets_expires_at", ExpireAfter = TimeSpan.Zero }
            ),
            options: null,
            cancellationToken: ct
        );

        // Query support
        await tickets.Indexes.CreateOneAsync(
            new CreateIndexModel<dynamic>(
                Builders<dynamic>.IndexKeys.Ascending("Status"),
                new CreateIndexOptions { Name = "idx_tickets_status" }
            ),
            options: null,
            cancellationToken: ct
        );
    }
}

