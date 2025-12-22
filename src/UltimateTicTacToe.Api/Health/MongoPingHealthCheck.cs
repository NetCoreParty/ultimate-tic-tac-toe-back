using Microsoft.Extensions.Diagnostics.HealthChecks;
using MongoDB.Bson;
using MongoDB.Driver;

namespace UltimateTicTacToe.API.Health;

public class MongoPingHealthCheck : IHealthCheck
{
    private readonly IMongoDatabase _db;

    public MongoPingHealthCheck(IMongoDatabase db)
    {
        _db = db;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var cmd = new BsonDocument("ping", 1);
            await _db.RunCommandAsync<BsonDocument>(cmd, cancellationToken: cancellationToken);
            return HealthCheckResult.Healthy("Mongo ping ok.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy($"Mongo ping failed: {ex.GetType().Name}: {ex.Message}");
        }
    }
}

