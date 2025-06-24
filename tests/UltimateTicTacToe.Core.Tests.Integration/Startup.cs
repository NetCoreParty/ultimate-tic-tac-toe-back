using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace UltimateTicTacToe.Core.Tests.Integration;

// Manual https://github.com/pengweiqhca/Xunit.DependencyInjection#4-default-startup
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddRealServices();
        services.AddInMemoryStorage();
    }
}

internal static class TestConfigurationExtensions
{
    internal static void AddRealServices(this IServiceCollection services)
    {
        
    }

    internal static void AddInMemoryStorage(this IServiceCollection services)
    {

    }
}