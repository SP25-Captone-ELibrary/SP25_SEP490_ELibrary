using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace FPTU_ELibrary.Application.HealthChecks;

public class ApiHealthCheck : IHealthCheck
{
    private readonly HttpClient _httpClient;

    public ApiHealthCheck(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync("https://localhost:5001/api", cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                return HealthCheckResult.Healthy("API is reachable.");
            }
            else
            {
                return HealthCheckResult.Unhealthy($"API returned status code {response.StatusCode}.");
            }
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("API is unreachable.", ex);
        }
    }
}