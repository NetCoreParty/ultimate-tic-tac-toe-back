using UltimateTicTacToe.Core.Features.Game.Domain.Events;
using UltimateTicTacToe.Core.Services;
using UltimateTicTacToe.Storage.HostedServices;
using UltimateTicTacToe.Storage.Tests.Integration.Infrastructure;

namespace UltimateTicTacToe.Storage.Tests.Integration.Services;

[Collection("MongoEventStoreTests")]
public class MongoEventStoreTests : IAsyncLifetime
{
    private readonly IEventStore _eventStore;
    private readonly EventStoreInitializer _storeInitializer;
    private readonly Guid _testGameId = Guid.NewGuid();

    public MongoEventStoreTests(
        IEventStore eventStore,
        EventStoreInitializer storeInitializer
        )
    {
        _eventStore = eventStore;
        _storeInitializer = storeInitializer;
    }

    public async Task InitializeAsync()
    {
        await _storeInitializer.StartAsync(default);
    }

    public async Task DisposeAsync()
    {
        await _eventStore.DeleteEventsByAsync(_testGameId);
        await _storeInitializer.ClearIndexesAsync();
        await _storeInitializer.ClearDatabaseAsync();
    }

    [Fact]
    public async Task Append_And_GetAllEvents_Works()
    {
        // Arrange
        var events = new List<IDomainEvent>
        {
            new FakeDomainEvent("Test1", DateTime.UtcNow) { Version = 1 },
            new FakeDomainEvent("Test2", DateTime.UtcNow) { Version = 2 }
        };

        // Act
        await _eventStore.AppendEventsAsync(_testGameId, events);

        var loadedEvents = await _eventStore.GetAllEventsAsync(_testGameId);

        // Assert
        Assert.Equal(2, loadedEvents.Count);
        Assert.Contains(loadedEvents, e => ((FakeDomainEvent)e).Name == "Test1");
    }

    [Fact]
    public async Task GetEventsAfterVersion_Should_ReturnCorrectEvents()
    {
        // Arrange
        var events = new List<IDomainEvent>
        {
            new FakeDomainEvent("Test1", DateTime.UtcNow) { Version = 1 },
            new FakeDomainEvent("Test2", DateTime.UtcNow) { Version = 2 }
        };

        // Act
        await _eventStore.AppendEventsAsync(_testGameId, events);

        var result = await _eventStore.GetEventsAfterVersionAsync(_testGameId, 1);

        // Assert
        Assert.Single(result);
        Assert.Equal("Test2", ((FakeDomainEvent)result[0]).Name);
    }
}