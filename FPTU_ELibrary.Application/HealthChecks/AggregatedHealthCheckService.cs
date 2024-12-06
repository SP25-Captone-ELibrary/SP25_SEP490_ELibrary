using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace FPTU_ELibrary.Application.HealthChecks;

//  TODO: Improve code for health checks
public class AggregatedHealthCheckService
{
    private readonly HealthCheckService _healthCheckService;

    public AggregatedHealthCheckService(HealthCheckService healthCheckService)
    {
        _healthCheckService = healthCheckService;
    }

    public async Task<Dictionary<string, HealthReportEntry>> GetHealthStatusAsync(CancellationToken cancellationToken = default)
    {
        var report = await _healthCheckService.CheckHealthAsync(cancellationToken);
        return report.Entries.ToDictionary(entry => 
            entry.Key, entry => entry.Value);
    }
}