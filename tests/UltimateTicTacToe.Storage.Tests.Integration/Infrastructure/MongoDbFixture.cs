using Mongo2Go;
using MongoDB.Driver;

namespace UltimateTicTacToe.Storage.Tests.Integration.Infrastructure;

public class MongoDbFixture : IDisposable
{
    public IMongoDatabase Database { get; private set; }
    public string DatabaseName { get; private set; } = "UltimateTicTacToe.Storage.Tests.Integration.Database";
    public string CollectionName { get; private set; } = "UltimateTicTacToe.Storage.Tests.Integration.Collection";
    public string ConnectionString { get; private set; }

    private readonly MongoDbRunner _runner;

    public MongoDbFixture()
    {
        _runner = MongoDbRunner.Start(singleNodeReplSet: true); // required if using transactions
        var client = new MongoClient(_runner.ConnectionString);
        Database = client.GetDatabase(DatabaseName);
        ConnectionString = _runner.ConnectionString;
    }

    public void Dispose()
    {
        _runner.Dispose();
    }
}