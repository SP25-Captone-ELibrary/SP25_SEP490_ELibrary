using Azure;
using FPTU_ELibrary.API.Payloads;
using FPTU_ELibrary.Application.Configurations;
using FPTU_ELibrary.Application.Dtos.Fine;
using FPTU_ELibrary.Domain.Interfaces.Services;
using FPTU_ELibrary.Domain.Specifications;
using FPTU_ELibrary.Domain.Specifications.Params;
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
    public async Task<IActionResult> GetAllFinePolicy([FromQuery] FinePolicyParams finePolicyParams)
    {
        return Ok(await _finePolicyService.GetAllWithSpecAsync(new FinePolicySpecification(
            finePolicyParams: finePolicyParams,
            pageIndex: finePolicyParams.PageIndex ?? 1,
            pageSize: finePolicyParams.PageSize ?? _appSettings.PageSize), tracked: false));
    }

    [HttpGet(APIRoute.FinePolicy.GetById, Name = nameof(GetFinePolicyById))]
    public async Task<IActionResult> GetFinePolicyById(int id)
    {
        return Ok(await _finePolicyService.GetByIdAsync(id));
    }

    [HttpPost(APIRoute.FinePolicy.Create, Name = nameof(CreateFinePolicy))]
    public async Task<IActionResult> CreateFinePolicy([FromBody] FinePolicyDto finePolicyDto)
    {
        return Ok(await _finePolicyService.CreateAsync(finePolicyDto));
    }

    // [HttpPut(APIRoute.FinePolicy.Update, Name = nameof(UpdateFinePolicy))]
    // public async Task<IActionResult> UpdateFinePolicy([FromBody] FinePolicyDto finePolicyDto)
    // {
    //     return Ok(await _finePolicyService.UpdateAsync(finePolicyDto));
    // }

    [HttpDelete(APIRoute.FinePolicy.HardDelete, Name = nameof(DeleteFinePolicy))]
    public async Task<IActionResult> DeleteFinePolicy(int id)
    {
        return Ok(await _finePolicyService.DeleteAsync(id));
    }
}