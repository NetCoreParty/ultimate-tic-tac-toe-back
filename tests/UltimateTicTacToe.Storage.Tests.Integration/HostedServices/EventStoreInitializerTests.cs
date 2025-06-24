using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Moq;
using UltimateTicTacToe.Core.Configuration;
using UltimateTicTacToe.Storage.HostedServices;
using UltimateTicTacToe.Storage.Tests.Integration.Infrastructure;

namespace UltimateTicTacToe.Storage.Tests.Integration.HostedServices;

[Collection("MongoEventStoreTests")]
public class EventStoreInitializerTests : IClassFixture<MongoDbFixture>, IAsyncLifetime
{
    private readonly EventStoreInitializer _sut;

    private readonly IMongoDatabase _dbFixture;
    private readonly Mock<ILogger<EventStoreInitializer>> _loggerMock = new Mock<ILogger<EventStoreInitializer>>();

    public EventStoreInitializerTests(MongoDbFixture fixture)
    {
        var settings = Options.Create(new EventStoreSettings
        {
            ConnectionString = fixture.Runner.ConnectionString,
            DatabaseName = fixture.DatabaseName,
            EventsCollectionName = fixture.CollectionName
        });

        _dbFixture = fixture.Database;

        _sut = new EventStoreInitializer(_dbFixture, settings, _loggerMock.Object);
    }

    public async Task InitializeAsync()
    {
        await _sut.StartAsync(default);
    }

    public async Task DisposeAsync()
    {
        await _sut.ClearIndexesAsync();
        await _sut.ClearDatabaseAsync();
    }

    [Fact]
    public async Task Indexes_ShouldExist_AfterInitFinished()
    {
        // Arrange & Act
        var appliedIndexes = await _sut.GetAppliedIndexesInfo();

        // Assert
        Assert.Equal(4, appliedIndexes.Count);

        Assert.True(EventStoreTestExtensions.IndexExists(appliedIndexes,
            "_id_",
            new Dictionary<string, int> { { "_id", 1 }, },
            unique: true)
            );

        Assert.True(EventStoreTestExtensions.IndexExists(appliedIndexes,
            "idx_aggregate_id",
            new Dictionary<string, int> { { "AggregateId", 1 }, },
            unique: false)
            );

        Assert.True(EventStoreTestExtensions.IndexExists(appliedIndexes,
            "idx_aggregate_id__event_version",
            new Dictionary<string, int> { { "AggregateId", 1 }, { "EventVersion", 1 } }, // 1 ASC, -1 DESC
            unique: false)
            );

        Assert.True(EventStoreTestExtensions.IndexExists(appliedIndexes,
            "idx_occurred_on",
            new Dictionary<string, int> { { "OccurredOn", 1 }, },
            unique: false)
            );
    }
}