using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using UltimateTicTacToe.Core.Features.GamePlay;
using UltimateTicTacToe.Core.Projections;
using UltimateTicTacToe.Core.Services;

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
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(MakeMoveCommand).Assembly));
    }

    internal static void AddInMemoryStorage(this IServiceCollection services)
    {
        services.AddSingleton<IGameRepository>(new FakeGameRepository(gamesNow: 7));
    }
}

internal sealed class FakeGameRepository : IGameRepository
{
    private readonly int _gamesNow;

    public FakeGameRepository(int gamesNow)
    {
        _gamesNow = gamesNow;
    }

    public int GamesNow => _gamesNow;

    public Task<Result<StartGameResponse>> TryStartGameAsync(CancellationToken ct = default)
        => throw new NotSupportedException("Not needed for this test.");

    public Task<Result<StartGameResponse>> TryStartGameForPlayersAsync(Guid playerXId, Guid playerOId, Guid? gameId = null, CancellationToken ct = default)
        => throw new NotSupportedException("Not needed for this test.");

    public Task<Result<bool>> TryMakeMoveAsync(PlayerMoveRequest move, CancellationToken ct = default)
        => throw new NotSupportedException("Not needed for this test.");

    public Task<Result<bool>> TryClearFinishedGamesAsync(CancellationToken ct = default)
        => throw new NotSupportedException("Not needed for this test.");

    public Task<Result<FilteredMovesHistoryResponse>> GetMovesFilteredByAsync(Guid gameId, int skip, int take, CancellationToken ct)
        => throw new NotSupportedException("Not needed for this test.");
}