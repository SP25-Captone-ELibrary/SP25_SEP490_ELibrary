using Azure;
using FPTU_ELibrary.API.Extensions;
using FPTU_ELibrary.API.Payloads;
using FPTU_ELibrary.API.Payloads.Requests;
using FPTU_ELibrary.API.Payloads.Requests.Fine;
using FPTU_ELibrary.Application.Configurations;
using FPTU_ELibrary.Application.Dtos.Fine;
using FPTU_ELibrary.Domain.Interfaces.Services;
using FPTU_ELibrary.Domain.Specifications;
using FPTU_ELibrary.Domain.Specifications.Params;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace FPTU_ELibrary.API.Controllers;

public class FinePolicyController : ControllerBase
{
    private readonly IFinePolicyService<FinePolicyDto> _finePolicyService;
    private readonly AppSettings _appSettings;

    public FinePolicyController(IFinePolicyService<FinePolicyDto> finePolicyService
        , IOptionsMonitor<AppSettings> appSettings)
    {
        _finePolicyService = finePolicyService;
        _appSettings = appSettings.CurrentValue;
    }
    
    [HttpGet(APIRoute.FinePolicy.GetAll, Name = nameof(GetAllFinePolicy))]
    [Authorize]
    public async Task<IActionResult> GetAllFinePolicy([FromQuery] FinePolicyParams finePolicyParams)
    {
        return Ok(await _finePolicyService.GetAllWithSpecAsync(new FinePolicySpecification(
            finePolicyParams: finePolicyParams,
            pageIndex: finePolicyParams.PageIndex ?? 1,
            pageSize: finePolicyParams.PageSize ?? _appSettings.PageSize), tracked: false));
    }

    [HttpGet(APIRoute.FinePolicy.GetById, Name = nameof(GetFinePolicyById))]
    [Authorize]
    public async Task<IActionResult> GetFinePolicyById(int id)
    {
        return Ok(await _finePolicyService.GetByIdAsync(id));
    }

    [HttpPost(APIRoute.FinePolicy.Create, Name = nameof(CreateFinePolicy))]
    [Authorize]
    public async Task<IActionResult> CreateFinePolicy([FromBody] CreateFinePolicyRequest req)
    {
        return Ok(await _finePolicyService.CreateAsync(req.ToFinePolicyDto()));
    }

    [HttpPatch(APIRoute.FinePolicy.Update, Name = nameof(UpdateFinePolicy))]
    [Authorize]
    public async Task<IActionResult> UpdateFinePolicy([FromRoute] int id,[FromBody] UpdateFinePolicyRequest finePolicyDto)
    {
        return Ok(await _finePolicyService.UpdateAsync(id,finePolicyDto.ToFinePolicyDto()));
    }

    [HttpDelete(APIRoute.FinePolicy.HardDelete, Name = nameof(DeleteFinePolicy))]
    [Authorize]
    public async Task<IActionResult> DeleteFinePolicy(int id)
    {
        return Ok(await _finePolicyService.DeleteAsync(id));
    }
    
    [HttpDelete(APIRoute.FinePolicy.HardDeleteRange, Name = nameof(HardDeleteRangeFinePolicy))]
    [Authorize]
    public async Task<IActionResult> HardDeleteRangeFinePolicy([FromBody] DeleteRangeRequest<int> ids)
    {
        return Ok(await _finePolicyService.HardDeleteRangeAsync(ids.Ids));
    }
    
    [HttpPost(APIRoute.FinePolicy.Import, Name = nameof(ImportFinePolicyAsync))]
    [Authorize]
    public async Task<IActionResult> ImportFinePolicyAsync([FromForm] ImportFinePolicyRequest req)
    {
        return Ok(await _finePolicyService.ImportFinePolicyAsync(req.File, req.DuplicateHandle));
    }
}