using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestPlatform.TestHost;
using Mongo2Go;
using MongoDB.Driver;

namespace UltimateTicTacToe.API.Tests.Integration;

public class IntegrationTestWebFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public IMongoDatabase? Database { get; private set; }
    public string DatabaseName { get; private set; } = "UltimateTicTacToe.API.Tests.Integration.Database";
    public string CollectionName { get; private set; } = "UltimateTicTacToe.API.Tests.Integration.Collection";

    private MongoDbRunner? _mongoRunner;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        var client = new MongoClient(_mongoRunner?.ConnectionString);
        Database = client.GetDatabase(DatabaseName);

        builder.ConfigureTestServices(services =>
        {
            // Remove real IMongoDatabase
            var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IMongoDatabase));

            if (descriptor is not null)
            {
                services.Remove(descriptor);
            }

            // Inject test Mongo
            services.AddSingleton(Database);
        });
    }

    public Task InitializeAsync()
    {
        _mongoRunner = MongoDbRunner.Start(singleNodeReplSet: true); // required if using transactions
        return Task.CompletedTask;
    }

    public override ValueTask DisposeAsync()
    {
        _mongoRunner?.Dispose();
        return base.DisposeAsync();
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await DisposeAsync();
    }
}