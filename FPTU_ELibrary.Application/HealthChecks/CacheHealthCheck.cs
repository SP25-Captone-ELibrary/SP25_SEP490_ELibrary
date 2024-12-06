using Microsoft.Extensions.Diagnostics.HealthChecks;
using StackExchange.Redis;

namespace FPTU_ELibrary.Application.HealthChecks;

public class CacheHealthCheck : IHealthCheck
{
    private readonly IConnectionMultiplexer _redisConnection;

    public CacheHealthCheck(IConnectionMultiplexer redisConnection)
    {
        _redisConnection = redisConnection;
    }

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, 
        CancellationToken cancellationToken = default)
    {
        if (_redisConnection.IsConnected)
        {
            return Task.FromResult(HealthCheckResult.Healthy("Cache is reachable."));
        }
        else
        {
            return Task.FromResult(HealthCheckResult.Unhealthy("Cache is unreachable."));
        }
    }
}