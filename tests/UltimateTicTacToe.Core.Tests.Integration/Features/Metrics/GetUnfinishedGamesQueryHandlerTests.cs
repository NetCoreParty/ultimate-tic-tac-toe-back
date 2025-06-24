using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestPlatform.TestHost;

namespace UltimateTicTacToe.Core.Tests.Integration.Features.Metrics;

public class GetUnfinishedGamesQueryHandlerTests
{
}

//public class TestApiFactory : WebApplicationFactory<Program>
//{
//    protected override void ConfigureWebHost(IWebHostBuilder builder)
//    {
//        builder.ConfigureAppConfiguration((context, config) =>
//        {
//            config.AddInMemoryCollection(new Dictionary<string, string>
//            {
//                ["CorsSettings:PolicyName"] = "SecureCorsPolicy",
//                ["CorsSettings:AllowedOrigins:0"] = "http://localhost:8080",
//                ["CorsSettings:AllowedOrigins:1"] = "https://your-production-url.com:12345"
//            });
//        });
//    }
//}