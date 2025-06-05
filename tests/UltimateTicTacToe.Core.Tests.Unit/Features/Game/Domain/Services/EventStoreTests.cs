using UltimateTicTacToe.Core.Domain.Events;
using UltimateTicTacToe.Core.Services;
using UltimateTicTacToe.Core.Tests.Unit.Infrastructure;

namespace UltimateTicTacToe.Core.Tests.Unit.Features.Game.Domain.Services;

public class InMemoryEventStoreTests
{
    private readonly InMemoryEventStore _eventStore;

    public InMemoryEventStoreTests()
    {
        _eventStore = new InMemoryEventStore();
    }

    [Fact]
    public async Task AppendEventsAsync_ShouldStoreEvents()
    {
        // Arrange
        var aggregateId = Guid.NewGuid();
        var events = new List<IDomainEvent>
        {
            new FakeDomainEvent("Event1", DateTime.UtcNow),
            new FakeDomainEvent("Event2", DateTime.UtcNow)
        };

        // Act
        await _eventStore.AppendEventsAsync(aggregateId, events);
        var storedEvents = await _eventStore.GetAllEventsAsync(aggregateId);

        // Assert
        Assert.Equal(2, storedEvents.Count);
        Assert.Contains(storedEvents, e => ((FakeDomainEvent)e).Name == "Event1");
        Assert.Contains(storedEvents, e => ((FakeDomainEvent)e).Name == "Event2");
    }

    [Fact]
    public async Task GetAllEvents_ShouldReturnEmptyList_WhenNoEvents()
    {
        // Arrange
        var nonExistentAggregateId = Guid.NewGuid();

        // Act
        var events = await _eventStore.GetAllEventsAsync(nonExistentAggregateId);

        // Assert
        Assert.NotNull(events);
        Assert.Empty(events);
    }

    [Fact]
    public async Task GetEventsAfterVersion_ShouldReturnCorrectEvents()
    {
        // Arrange
        var aggregateId = Guid.NewGuid();
        var events = new List<IDomainEvent>
        {
            new FakeDomainEvent("Event1", DateTime.UtcNow),
            new FakeDomainEvent("Event2", DateTime.UtcNow),
            new FakeDomainEvent("Event3", DateTime.UtcNow),
        };

        await _eventStore.AppendEventsAsync(aggregateId, events);

        // Act
        var eventsAfterVersion1 = await _eventStore.GetEventsAfterVersionAsync(aggregateId, 1);

        // Assert
        Assert.Equal(2, eventsAfterVersion1.Count);
        Assert.Equal("Event2", ((FakeDomainEvent)eventsAfterVersion1[0]).Name);
        Assert.Equal("Event3", ((FakeDomainEvent)eventsAfterVersion1[1]).Name);
    }

    [Fact]
    public async Task GetEventsAfterVersion_ShouldReturnEmpty_WhenVersionIsLatest()
    {
        // Arrange
        var aggregateId = Guid.NewGuid();
        var events = new List<IDomainEvent>
        {
            new FakeDomainEvent("Event1", DateTime.UtcNow),
        };

        await _eventStore.AppendEventsAsync(aggregateId, events);

        // Act
        var eventsAfterVersion1 = await _eventStore.GetEventsAfterVersionAsync(aggregateId, 1);

        // Assert
        Assert.NotNull(eventsAfterVersion1);
        Assert.Empty(eventsAfterVersion1);
    }

    [Fact]
    public async Task AppendEventsAsync_ShouldIncrementVersionsCorrectly()
    {
        // Arrange
        var aggregateId = Guid.NewGuid();

        var events1 = new List<IDomainEvent>
        {
            new FakeDomainEvent("Event1", DateTime.UtcNow),
        };

        var events2 = new List<IDomainEvent>
        {
            new FakeDomainEvent("Event2", DateTime.UtcNow),
            new FakeDomainEvent("Event3", DateTime.UtcNow),
        };

        // Act
        await _eventStore.AppendEventsAsync(aggregateId, events1);
        await _eventStore.AppendEventsAsync(aggregateId, events2);

        var allEvents = await _eventStore.GetAllEventsAsync(aggregateId);

        // Assert
        Assert.Equal(3, allEvents.Count);
        Assert.Equal("Event1", ((FakeDomainEvent)allEvents[0]).Name);
        Assert.Equal("Event2", ((FakeDomainEvent)allEvents[1]).Name);
        Assert.Equal("Event3", ((FakeDomainEvent)allEvents[2]).Name);

        var eventsAfterFirst = await _eventStore.GetEventsAfterVersionAsync(aggregateId, 1);

        Assert.Equal(2, eventsAfterFirst.Count); // Only Event2 and Event3
    }

}