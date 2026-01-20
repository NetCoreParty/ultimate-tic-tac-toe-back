namespace UltimateTicTacToe.API.Tests.Integration.CORS;

public class CorsTests
{
    [Fact]
    public void JustTest()
    {
        Assert.True(false);
    }
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