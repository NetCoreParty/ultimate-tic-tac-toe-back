using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using UltimateTicTacToe.Core.Configuration;
using UltimateTicTacToe.Core.Domain.Entities;
using UltimateTicTacToe.Core.Domain.Events;
using UltimateTicTacToe.Core.Services;
using UltimateTicTacToe.Storage.Extensions;
using UltimateTicTacToe.Storage.Services;
using UltimateTicTacToe.Storage.Tests.Integration.Infrastructure;

namespace UltimateTicTacToe.Storage.Tests.Integration.Services;

[Collection("MongoEventStoreTests")]
public class MongoEventStorePolymorphismTests : IClassFixture<MongoDbFixture>, IAsyncLifetime
{
    private readonly IEventStore _sut;
    private readonly Guid _gameId = Guid.NewGuid();

    public MongoEventStorePolymorphismTests(MongoDbFixture fixture)
    {
        // Ensure global BSON maps are registered for all events
        new ServiceCollection().AddGlobalMongoSerialization();

        var settings = Options.Create(new EventStoreSettings
        {
            ConnectionString = fixture.Runner.ConnectionString,
            DatabaseName = fixture.DatabaseName,
            EventsCollectionName = fixture.CollectionName
        });

        _sut = new MongoEventStore(fixture.Database, settings);
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        await _sut.DeleteEventsByAsync(_gameId);
    }

    [Fact]
    public async Task Append_And_Load_AllDomainEventTypes_Works()
    {
        var p1 = Guid.NewGuid();
        var p2 = Guid.NewGuid();

        var events = new List<IDomainEvent>
        {
            new GameCreatedEvent(_gameId, p1, p2) { Version = 1 },
            new CellMarkedEvent(_gameId, p1, 0, 0, 0, 0, PlayerFigure.X) { Version = 2 },
            new MiniBoardWonEvent(_gameId, p1, 0, 0, PlayerFigure.X) { Version = 3 },
            new MiniBoardDrawnEvent(_gameId, 0, 1) { Version = 4 },
            new FullGameWonEvent(_gameId, p1) { Version = 5 },
            new GameDrawnEvent(_gameId) { Version = 6 },
        };

        await _sut.AppendEventsAsync(_gameId, events);

        var loaded = await _sut.GetAllEventsAsync(_gameId);

        Assert.Equal(events.Count, loaded.Count);
        Assert.IsType<GameCreatedEvent>(loaded[0]);
        Assert.IsType<CellMarkedEvent>(loaded[1]);
        Assert.IsType<MiniBoardWonEvent>(loaded[2]);
        Assert.IsType<MiniBoardDrawnEvent>(loaded[3]);
        Assert.IsType<FullGameWonEvent>(loaded[4]);
        Assert.IsType<GameDrawnEvent>(loaded[5]);
    }
}

