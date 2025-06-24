using Mongo2Go;
using MongoDB.Driver;

namespace UltimateTicTacToe.Storage.Tests.Integration.Infrastructure;

public class MongoDbFixture : IDisposable
{
    public MongoDbRunner Runner { get; }
    public IMongoDatabase Database { get; }
    public string DatabaseName { get; } = "InMemory_MongoDb_Test";
    public string CollectionName = "StoredEvents_Test";

    public MongoDbFixture()
    {
        Runner = MongoDbRunner.Start(singleNodeReplSet: true); // required if using transactions
        var client = new MongoClient(Runner.ConnectionString);
        Database = client.GetDatabase(DatabaseName);
    }

    public void Dispose()
    {
        Runner.Dispose();
    }
}