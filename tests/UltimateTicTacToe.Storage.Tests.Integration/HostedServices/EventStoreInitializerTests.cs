using UltimateTicTacToe.Storage.HostedServices;
using UltimateTicTacToe.Storage.Tests.Integration.Infrastructure;

namespace UltimateTicTacToe.Storage.Tests.Integration.HostedServices;

[Collection("MongoEventStoreTests")]
public class EventStoreInitializerTests : IAsyncLifetime
{
    private readonly EventStoreInitializer _storeInitializer;

    public EventStoreInitializerTests(
        EventStoreInitializer storeInitializer
        )
    {
        _storeInitializer = storeInitializer;
    }

    public async Task InitializeAsync()
    {
        await _storeInitializer.StartAsync(default);
    }

    public async Task DisposeAsync()
    {
        await _storeInitializer.ClearIndexesAsync();
        await _storeInitializer.ClearDatabaseAsync();
    }

    [Fact]
    public async Task Indexes_ShouldExist_AfterInitFinished()
    {
        // Arrange & Act
        var appliedIndexes = await _storeInitializer.GetAppliedIndexesInfo();

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