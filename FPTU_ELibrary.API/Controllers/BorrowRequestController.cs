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
using Nest;

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
    [Authorize]
    [HttpGet(APIRoute.BorrowRequest.GetAllManagement, Name = nameof(GetAllManagementAsync))]
    public async Task<IActionResult> GetAllManagementAsync([FromQuery] BorrowRequestSpecParams specParams)
    {
        return Ok(await _borrowReqSvc.GetAllWithSpecAsync(new BorrowRequestSpecification(
            specParams: specParams,
            pageIndex: specParams.PageIndex ?? 1,
            pageSize: specParams.PageSize ?? _appSettings.PageSize)));
    }

    [Authorize]
    [HttpGet(APIRoute.BorrowRequest.GetByIdManagement, Name = nameof(GetByIdManagementAsync))]
    public async Task<IActionResult> GetByIdManagementAsync([FromRoute] int id)
    {
        return Ok(await _borrowReqSvc.GetByIdAsync(id));
    }
    
    [Authorize]
    [HttpGet(APIRoute.BorrowRequest.CheckExistBarcode, Name = nameof(CheckExistBarcodeInRequestAsync))]
    public async Task<IActionResult> CheckExistBarcodeInRequestAsync([FromRoute] int id, [FromQuery] string barcode)
    {
        return Ok(await _borrowReqSvc.CheckExistBarcodeInRequestAsync(id: id, barcode: barcode));
    }
    #endregion
    
    [Authorize]    
    [HttpPost(APIRoute.BorrowRequest.Create, Name = nameof(CreateBorrowRequestAsync))]
    public async Task<IActionResult> CreateBorrowRequestAsync([FromBody] CreateBorrowRequest req)
    {
        var email = HttpContext.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
        return Ok(await _borrowReqSvc.CreateAsync(
            email: email ?? string.Empty,
            dto: req.ToBorrowRequestDto(),
            reservationIds: req.ReservationItemIds,
            userFavoriteIds: req.UserFavoriteItemIds));
    }
    
    [Authorize]    
    [HttpPatch(APIRoute.BorrowRequest.Cancel, Name = nameof(CancelBorrowRequestAsync))]
    public async Task<IActionResult> CancelBorrowRequestAsync([FromRoute] int id, [FromQuery] string? cancellationReason = null)
    {
        var email = HttpContext.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
        return Ok(await _borrowReqSvc.CancelAsync(
            email: email ?? string.Empty,
            id: id, 
            cancellationReason: cancellationReason));
    }
}