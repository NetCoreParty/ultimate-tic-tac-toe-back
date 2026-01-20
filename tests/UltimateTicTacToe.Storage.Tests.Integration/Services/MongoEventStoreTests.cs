using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Moq;
using UltimateTicTacToe.Core.Configuration;
using UltimateTicTacToe.Core.Domain.Events;
using UltimateTicTacToe.Core.Services;
using UltimateTicTacToe.Storage.HostedServices;
using UltimateTicTacToe.Storage.Services;
using UltimateTicTacToe.Storage.Tests.Integration.Infrastructure;

namespace UltimateTicTacToe.Storage.Tests.Integration.Services;

[Collection("MongoEventStoreTests")]
public class MongoEventStoreTests : IClassFixture<MongoDbFixture>, IAsyncLifetime
{
    private readonly IEventStore _sut;

    private readonly EventStoreInitializer _realStoreInitializer;
    private readonly Guid _testGameId = Guid.NewGuid();
    private readonly IMongoDatabase _dbFixture;
    private readonly Mock<ILogger<EventStoreInitializer>> _loggerMock = new Mock<ILogger<EventStoreInitializer>>();

    public MongoEventStoreTests(MongoDbFixture fixture)
    {
        var settings = Options.Create(new EventStoreSettings
        {
            ConnectionString = fixture.ConnectionString,
            DatabaseName = fixture.DatabaseName,
            EventsCollectionName = fixture.CollectionName
        });

        _dbFixture = fixture.Database;
        _realStoreInitializer = new EventStoreInitializer(_dbFixture, settings, _loggerMock.Object);
        _sut = new MongoEventStore(_dbFixture, settings);
    }

    public async Task InitializeAsync()
    {
        await _realStoreInitializer.StartAsync(CancellationToken.None);
    }

    public async Task DisposeAsync()
    {
        await _sut.DeleteEventsByAsync(_testGameId);
        await _realStoreInitializer.ClearIndexesAsync();
        await _realStoreInitializer.ClearDatabaseAsync();
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
        await _sut.AppendEventsAsync(_testGameId, events);

        var loadedEvents = await _sut.GetAllEventsAsync(_testGameId);

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
        await _sut.AppendEventsAsync(_testGameId, events);

        var result = await _sut.GetEventsAfterVersionAsync(_testGameId, 1);

        // Assert
        Assert.Single(result);
        Assert.Equal("Test2", ((FakeDomainEvent)result[0]).Name);
    }
}