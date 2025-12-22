using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using UltimateTicTacToe.Core.Configuration;
using UltimateTicTacToe.Core.Domain.Aggregate;
using UltimateTicTacToe.Core.Domain.Entities;
using UltimateTicTacToe.Core.Domain.Events;
using UltimateTicTacToe.Core.Features.GameSaving;
using UltimateTicTacToe.Core.Services;
using UltimateTicTacToe.Storage.HostedServices;
using UltimateTicTacToe.Storage.Services;
using UltimateTicTacToe.Storage.Tests.Integration.Infrastructure;
using Xunit;

namespace UltimateTicTacToe.Storage.Tests.Integration.Services;

[Collection("MongoEventStoreTests")]
public class MongoStateSnapshotStoreTests : IClassFixture<MongoDbFixture>, IAsyncLifetime
{
    private readonly IMongoDatabase _db;
    private readonly EventStoreInitializer _initializer;
    private readonly IEventStore _eventStore;
    private readonly IStateSnapshotStore _snapshots;

    public MongoStateSnapshotStoreTests(MongoDbFixture fixture)
    {
        _db = fixture.Database;

        var eventSettings = Options.Create(new EventStoreSettings
        {
            ConnectionString = fixture.Runner.ConnectionString,
            DatabaseName = fixture.DatabaseName,
            EventsCollectionName = fixture.CollectionName
        });

        _initializer = new EventStoreInitializer(_db, eventSettings, new NullLogger<EventStoreInitializer>());
        _eventStore = new MongoEventStore(_db, eventSettings);

        _snapshots = new MongoStateSnapshotStore(_db, Options.Create(new GameplaySettings { EventsUntilSnapshot = 2 }));
    }

    public async Task InitializeAsync()
    {
        await _initializer.StartAsync(CancellationToken.None);
    }

    public async Task DisposeAsync()
    {
        // Cleanup collections
        await _db.DropCollectionAsync(MongoStateSnapshotStore.CollectionName);
        await _initializer.ClearDatabaseAsync();
    }

    [Fact]
    public async Task TryCreateAndLoadSnapshot_ShouldReturnSnapshotPlusDelta()
    {
        // Arrange
        var gameId = Guid.NewGuid();
        var playerX = Guid.NewGuid();
        var playerO = Guid.NewGuid();

        var game = GameRoot.CreateNew(gameId, playerX, playerO);
        await _eventStore.AppendEventsAsync(gameId, game.UncommittedChanges, CancellationToken.None);
        GameRoot.ClearUncomittedEvents(game);

        // Make two moves to trigger snapshot threshold
        game.PlayMove(playerX, 0, 0, 0, 0);
        await _eventStore.AppendEventsAsync(gameId, game.UncommittedChanges, CancellationToken.None);
        await _snapshots.TryCreateSnapshotAsync(game);
        GameRoot.ClearUncomittedEvents(game);

        game.PlayMove(playerO, 0, 0, 0, 1);
        await _eventStore.AppendEventsAsync(gameId, game.UncommittedChanges, CancellationToken.None);
        GameRoot.ClearUncomittedEvents(game);

        // Act
        var loaded = await _snapshots.TryLoadGameAsync(gameId, _eventStore, CancellationToken.None);

        // Assert
        Assert.NotNull(loaded);
        Assert.Equal(gameId, loaded!.GameId);
        Assert.True(loaded.Version >= 2);

        var mini = loaded.Board.GetMiniBoard(0, 0);
        Assert.Equal(PlayerFigure.X, mini.GetCell(0, 0).Figure);
        Assert.Equal(PlayerFigure.O, mini.GetCell(0, 1).Figure);
    }

    [Fact]
    public async Task SnapshotIndexes_ShouldExist()
    {
        var collection = _db.GetCollection<MongoStateSnapshotStore.SnapshotDoc>(MongoStateSnapshotStore.CollectionName);
        var cursor = await collection.Indexes.ListAsync();
        var indexes = await cursor.ToListAsync();

        Assert.Contains(indexes, i => i["name"] == "idx_game_id__version_desc");
        Assert.Contains(indexes, i => i["name"] == "uidx_game_id__version");
    }
}

