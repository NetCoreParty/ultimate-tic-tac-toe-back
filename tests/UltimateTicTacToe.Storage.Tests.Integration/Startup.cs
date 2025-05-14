using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Bson.Serialization;
using MongoDB.Bson;
using UltimateTicTacToe.Core.Configuration;
using UltimateTicTacToe.Core.Features.Game.Domain.Events;
using UltimateTicTacToe.Storage.HostedServices;
using UltimateTicTacToe.Storage.Services;
using UltimateTicTacToe.Storage.Tests.Integration.Services;
using UltimateTicTacToe.Core.Features.Gameplay;

namespace UltimateTicTacToe.Storage.Tests.Integration;

// Manual https://github.com/pengweiqhca/Xunit.DependencyInjection#4-default-startup
public class Startup
{
    public IConfiguration AppConfiguration { get; set; }

    public Startup()
    {
        var builder = new ConfigurationBuilder();
        builder.AddTestJsonConfig();
        AppConfiguration = builder.Build();
    }

    public void ConfigureServices(IServiceCollection services)
    {
        var eventStoreConfig = new EventStoreSettings();
        AppConfiguration.GetSection("EventStoreSettings").Bind(eventStoreConfig);

        services.AddTestEventStore(AppConfiguration, eventStoreConfig);
    }
}

internal static class TestConfigurationExtensions
{
    internal static void AddTestJsonConfig(this IConfigurationBuilder configuration)
    {
        var testConfig = Path.Combine(AppContext.BaseDirectory, "appsettings.Integration.Tests.json");
        configuration.AddJsonFile(testConfig, optional: false);
    }

    internal static void AddTestEventStore(this IServiceCollection services, IConfiguration applicationConfig, EventStoreSettings eventStoreConfig)
    {
        services.Configure<EventStoreSettings>(applicationConfig.GetSection("EventStoreSettings"));

        BsonSerializer.RegisterSerializer(new GuidSerializer(GuidRepresentation.Standard));
        // Register base class and its known types
        BsonClassMap.RegisterClassMap<DomainEventBase>(cm =>
        {
            cm.AutoMap();
            cm.SetIsRootClass(true);
        });

        BsonClassMap.RegisterClassMap<CellMarkedEvent>(cm => cm.AutoMap());

        services.AddSingleton<IEventStore, MongoEventStore>();
        services.AddSingleton<EventStoreInitializer>();
    }
}