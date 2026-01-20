using Microsoft.Extensions.DependencyInjection;

namespace UltimateTicTacToe.API.Tests.Integration;

public abstract class BaseIntegrationTest
    : IClassFixture<IntegrationTestWebFactory>,
      IDisposable
{

    private readonly IServiceScope _scope;
    // protected readonly ISender Sender;

    public void Dispose()
    {
        throw new NotImplementedException();
    }
}
