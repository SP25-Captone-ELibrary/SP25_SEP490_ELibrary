using System.Security.Claims;
using FPTU_ELibrary.API.Extensions;
using FPTU_ELibrary.API.Payloads;
using FPTU_ELibrary.API.Payloads.Requests.Borrow;
using FPTU_ELibrary.Application.Dtos.Borrows;
using FPTU_ELibrary.Domain.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FPTU_ELibrary.API.Controllers;

[ApiController]
public class BorrowRecordController : ControllerBase
{
    private readonly IBorrowRecordService<BorrowRecordDto> _borrowRecSvc;

    public BorrowRecordController(IBorrowRecordService<BorrowRecordDto> borrowRecSvc)
    {
        _borrowRecSvc = borrowRecSvc;
    }
    
    #region Management
    
    [Authorize]
    [HttpPost(APIRoute.BorrowRecord.ProcessRequest, Name = nameof(ProcessRequestToBorrowRecordAsync))]
    public async Task<IActionResult> ProcessRequestToBorrowRecordAsync([FromBody] ProcessToBorrowRecordRequest req)
    {
        var email = HttpContext.User.Claims.FirstOrDefault(claim => claim.Type == ClaimTypes.Email)?.Value;
        return Ok(await _borrowRecSvc.ProcessRequestToBorrowRecordAsync(
            processedByEmail: email ?? string.Empty, dto: req.ToBorrowRecordDto()));
    }
    
    #endregion
}