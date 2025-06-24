using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using UltimateTicTacToe.Storage.Extensions;

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
        services.AddGlobalMongoSerialization();
    }
}

internal static class TestConfigurationExtensions
{
    internal static void AddTestJsonConfig(this IConfigurationBuilder configuration)
    {
        var testConfig = Path.Combine(AppContext.BaseDirectory, "appsettings.Integration.Tests.json");
        configuration.AddJsonFile(testConfig, optional: false);
    }
}