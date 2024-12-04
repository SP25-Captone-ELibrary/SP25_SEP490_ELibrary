using FPTU_ELibrary.API.Payloads;
using FPTU_ELibrary.API.Payloads.Requests.SystemMessage;
using FPTU_ELibrary.Application.Dtos;
using FPTU_ELibrary.Domain.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FPTU_ELibrary.API.Controllers;

[ApiController]
public class SystemMessageController : ControllerBase
{
    private readonly ISystemMessageService _systemMsgService;

    public SystemMessageController(
        ISystemMessageService systemMsgService)
    {
        _systemMsgService = systemMsgService;
    }

    [AllowAnonymous]
    [HttpPost(APIRoute.SystemMessage.ImportToExcel, Name = nameof(ImportMessagesAsync))]
    public async Task<IActionResult> ImportMessagesAsync([FromForm] ImportMessageRequest req)
    {
        return Ok(await _systemMsgService.ImportToExcelAsync(req.File));
    }
}