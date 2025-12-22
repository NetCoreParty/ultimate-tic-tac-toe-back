using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using UltimateTicTacToe.Core.Configuration;
using UltimateTicTacToe.Core.Features.Rooms;

namespace UltimateTicTacToe.API.HostedServices;

/// <summary>
/// Best-effort sweeper to notify users about expired queue tickets / half-full waiting rooms.
/// Mongo TTL deletion won't emit application-level events, so we do it here.
/// </summary>
public class RoomsExpiryWorker : BackgroundService
{
    private readonly RoomsExpirySweeper _sweeper;
    private readonly RoomsSweepSettings _settings;
    private readonly ILogger<RoomsExpiryWorker> _logger;

    public RoomsExpiryWorker(RoomsExpirySweeper sweeper, IOptions<RoomsSweepSettings> settings, ILogger<RoomsExpiryWorker> logger)
    {
        _sweeper = sweeper;
        _settings = settings.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await SweepOnceAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // shutdown
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "RoomsExpiryWorker sweep failed.");
            }

            try
            {
                var delaySeconds = _settings.IntervalSeconds <= 0 ? 5 : _settings.IntervalSeconds;
                await Task.Delay(TimeSpan.FromSeconds(delaySeconds), stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // shutdown
            }
        }
    }

    private async Task SweepOnceAsync(CancellationToken ct)
    {
        var nowUtc = DateTime.UtcNow;
        var batchSize = _settings.BatchSize <= 0 ? 200 : _settings.BatchSize;
        await _sweeper.SweepOnceAsync(nowUtc, batchSize, ct);
    }
}

