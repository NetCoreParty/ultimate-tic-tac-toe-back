using Microsoft.Extensions.DependencyInjection;

namespace UltimateTicTacToe.API.Tests.Integration;

// Manual https://github.com/pengweiqhca/Xunit.DependencyInjection#4-default-startup
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddInMemoryStorage();
    }
}

internal static class TestConfigurationExtensions
{
    internal static void AddInMemoryStorage(this IServiceCollection services)
    {

    }
}