using MediatR;
using UltimateTicTacToe.Core.Features.Metrics;

namespace UltimateTicTacToe.Core.Tests.Integration.Features.Metrics;

public class GetUnfinishedGamesQueryHandlerTests
{
    private readonly IMediator _mediator;

    public GetUnfinishedGamesQueryHandlerTests(IMediator mediator)
    {
        _mediator = mediator;
    }

    [Fact]
    public async Task ShouldReturn_GamesNow_FromRepository()
    {
        var result = await _mediator.Send(new GetUnfinishedGamesQuery(), CancellationToken.None);
        Assert.True(result.IsSuccess);
        Assert.Equal(7, result.Value);
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