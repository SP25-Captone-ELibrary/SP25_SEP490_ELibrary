using FPTU_ELibrary.API.Payloads;
using FPTU_ELibrary.Application.Dtos.AdminConfiguration;
using FPTU_ELibrary.Application.Services.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace FPTU_ELibrary.API.Controllers;

[ApiController]
public class AdminConfigurationController : ControllerBase
{
    private readonly IAdminConfigurationService _adminConfigurationService;

    public AdminConfigurationController(IAdminConfigurationService adminConfigurationService)
    {
        _adminConfigurationService = adminConfigurationService;
    }

    // base on the service function, create the suitable api

    [HttpGet(APIRoute.AdminConfiguration.GetAll, Name = nameof(GetAllKeyVault))]
    [Authorize]
    public async Task<IActionResult> GetAllKeyVault()
    {
        return Ok(await _adminConfigurationService.GetAllInAzureConfiguration());
    }

    [HttpGet(APIRoute.AdminConfiguration.GetDetail, Name = nameof(GetKeyVault))]
    [Authorize]
    public async Task<IActionResult> GetKeyVault([FromRoute] string name)
    {
        return Ok(await _adminConfigurationService.GetKeyVault(name));
    }

    [HttpPatch(APIRoute.AdminConfiguration.Update, Name = nameof(UpdateKeyVault))]
    [Authorize]
    public async Task<IActionResult> UpdateKeyVault([FromBody] UpdateKeyValueAzureConfigurationDto dto)
    {
        return Ok(await _adminConfigurationService.UpdateKeyValueAzureConfiguration(dto.Name, dto.Value));
    }
    
    [HttpPatch(APIRoute.AdminConfiguration.UpdateLibrarySchedule, Name = nameof(UpdateLibrarySchedule))]
    [Authorize]
    public async Task<IActionResult> UpdateLibrarySchedule(
        [FromBody] List<WorkDateAndTime> dto)
    {
        return Ok(await _adminConfigurationService.UpdateLibraryScheduleAsync(dto));
    }
            
}