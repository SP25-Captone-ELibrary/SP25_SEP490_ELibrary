using System.Security.Claims;
using FPTU_ELibrary.API.Extensions;
using FPTU_ELibrary.API.Payloads;
using FPTU_ELibrary.API.Payloads.Requests.Borrow;
using FPTU_ELibrary.Application.Configurations;
using FPTU_ELibrary.Application.Dtos.Borrows;
using FPTU_ELibrary.Domain.Interfaces.Services;
using FPTU_ELibrary.Domain.Specifications;
using FPTU_ELibrary.Domain.Specifications.Params;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace FPTU_ELibrary.API.Controllers;

[ApiController]
public class BorrowRequestController : ControllerBase
{
    private readonly IBorrowRequestService<BorrowRequestDto> _borrowReqSvc;
    private readonly AppSettings _appSettings;

    public BorrowRequestController(
        IBorrowRequestService<BorrowRequestDto> borrowReqSvc,
        IOptionsMonitor<AppSettings> monitor)
    {
        _borrowReqSvc = borrowReqSvc;
        _appSettings = monitor.CurrentValue;
    }
    
    #region Management
    #endregion

    [Authorize]
    [HttpGet(APIRoute.BorrowRequest.GetAll, Name = nameof(GetAllBorrowRequestWithEmailAsync))]
    public async Task<IActionResult> GetAllBorrowRequestWithEmailAsync([FromQuery] BorrowRequestSpecParams specParams)
    {
        var email = HttpContext.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
        return Ok(await _borrowReqSvc.GetAllByEmailAsync(
            email: email ?? string.Empty,
            spec: new BorrowRequestSpecification(
                specParams: specParams,
                pageIndex: specParams.PageIndex ?? 1,
                pageSize: specParams.PageSize ?? _appSettings.PageSize)));
    }

    [HttpGet(APIRoute.BorrowRequest.GetById, Name = nameof(GetBorrowRequestByIdWithEmailAsync))]
    public async Task<IActionResult> GetBorrowRequestByIdWithEmailAsync([FromRoute] int id)
    {
        var email = HttpContext.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
        return Ok(await _borrowReqSvc.GetByIdAsync(id: id, email: email ?? string.Empty));
    }
    
    [Authorize]    
    [HttpPost(APIRoute.BorrowRequest.Create, Name = nameof(CreateBorrowRequestAsync))]
    public async Task<IActionResult> CreateBorrowRequestAsync([FromBody] CreateBorrowRequest req)
    {
        var email = HttpContext.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
        return Ok(await _borrowReqSvc.CreateAsync(email ?? string.Empty, req.ToBorrowRequestDto()));
    }
    
    [Authorize]    
    [HttpPatch(APIRoute.BorrowRequest.Cancel, Name = nameof(CancelBorrowRequestAsync))]
    public async Task<IActionResult> CancelBorrowRequestAsync([FromRoute] int id, [FromQuery] string? cancellationReason = null)
    {
        var email = HttpContext.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
        return Ok(await _borrowReqSvc.CancelAsync(
            email: email ?? string.Empty,
            borrowRequestId: id, 
            cancellationReason: cancellationReason));
    }
}