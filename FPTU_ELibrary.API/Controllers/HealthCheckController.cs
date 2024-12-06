using System.Net;
using FPTU_ELibrary.API.Payloads;
using FPTU_ELibrary.Application.HealthChecks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace FPTU_ELibrary.API.Controllers;

[ApiController]
public class HealthCheckController : ControllerBase
{
    private readonly AggregatedHealthCheckService _aggregatedHealthCheckService;

    public HealthCheckController(AggregatedHealthCheckService aggregatedHealthCheckService)
    {
        _aggregatedHealthCheckService = aggregatedHealthCheckService;
    }

    [HttpGet(APIRoute.HealthCheck.BaseUrl)]
    public async Task<IActionResult> ForCheckAsync()
    {
        // Mark as complete task
        await Task.CompletedTask;
        return Ok();
    }
    
    [HttpGet(APIRoute.HealthCheck.Check)]
    public async Task<IActionResult> GetHealthStatusAsync()
    {
        var healthStatus = await _aggregatedHealthCheckService.GetHealthStatusAsync();

        var response = healthStatus.Select(entry => new
        {
            Name = entry.Key,
            Status = entry.Value.Status.ToString(),
            Description = entry.Value.Description
        });

        bool isHealthy = healthStatus.All(entry => entry.Value.Status == HealthStatus.Healthy);

        return isHealthy ? Ok(response) : StatusCode((int)HttpStatusCode.ServiceUnavailable, response);
    }
}