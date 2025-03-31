using FPTU_ELibrary.API.Payloads;
using FPTU_ELibrary.Application.Dtos.Borrows;
using FPTU_ELibrary.Domain.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;

namespace FPTU_ELibrary.API.Controllers;

[ApiController]
public class SystemConfigurationController : ControllerBase
{
    private readonly IBorrowRecordService<BorrowRecordDto> _borrowRecSvc;

    public SystemConfigurationController(IBorrowRecordService<BorrowRecordDto> borrowRecSvc)
    {
        _borrowRecSvc = borrowRecSvc;
    }
    
    [HttpGet(APIRoute.SystemConfiguration.GetBorrowSettings, Name = nameof(GetAllBorrowSettingValuesAsync))]
    public async Task<IActionResult> GetAllBorrowSettingValuesAsync()
    {
        return Ok(await _borrowRecSvc.GetAllBorrowSettingValuesAsync());
    }
}