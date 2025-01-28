using FPTU_ELibrary.API.Payloads;
using FPTU_ELibrary.Application.Configurations;
using FPTU_ELibrary.Application.Dtos.AuditTrail;
using FPTU_ELibrary.Domain.Common.Enums;
using FPTU_ELibrary.Domain.Interfaces.Services;
using FPTU_ELibrary.Domain.Specifications;
using FPTU_ELibrary.Domain.Specifications.Params;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace FPTU_ELibrary.API.Controllers;

[ApiController]
public class AuditTrailController : ControllerBase
{
    private readonly IAuditTrailService<AuditTrailDto> _auditService;
    private readonly AppSettings _appSettings;

    public AuditTrailController(
        IAuditTrailService<AuditTrailDto> auditService,
        IOptionsMonitor<AppSettings> monitor)
    {
        _auditService = auditService;
        _appSettings = monitor.CurrentValue;
    }
    
    [Authorize]
    [HttpGet(APIRoute.AuditTrail.GetAllByEntityIdAndName, Name = nameof(GetAllAuditTrailByIdAsync))]
    public async Task<IActionResult> GetAllAuditTrailByIdAsync([FromQuery] AuditTrailSpecParams specParams)
    {
        // Retrieve user email claims
        // var email = HttpContext.User.FindFirst(x => x.Type == ClaimTypes.Email)?.Value;
        return Ok(await _auditService.GetAllWithSpecAsync(new AuditTrailSpecification(
            specParams: specParams,
            pageIndex: specParams.PageIndex ?? 1,
            pageSize: specParams.PageSize ?? _appSettings.PageSize)));
    }

    [Authorize]
    [HttpGet(APIRoute.AuditTrail.GetDetailByDateUtc, Name = nameof(GetAuditTrailDetailByDateUtcAsync))]
    public async Task<IActionResult> GetAuditTrailDetailByDateUtcAsync([FromQuery] string dateUtc, 
        [FromQuery] string entityName, [FromQuery] TrailType trailType)
    {
        return Ok(await _auditService.GetAuditDetailByDateUtcAndEntityNameAsync(dateUtc, entityName, trailType));        
    }
}