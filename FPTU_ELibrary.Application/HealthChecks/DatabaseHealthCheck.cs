using System.Data.Common;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace FPTU_ELibrary.Application.HealthChecks;

public class DatabaseHealthCheck : IHealthCheck
{
    private readonly DbConnection _dbConnection;

    public DatabaseHealthCheck(DbConnection dbConnection)
    {
        _dbConnection = dbConnection;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _dbConnection.OpenAsync(cancellationToken);
            await _dbConnection.CloseAsync();
            return HealthCheckResult.Healthy("Database connection is healthy.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Database connection is unhealthy.", ex);
        }
    }
}