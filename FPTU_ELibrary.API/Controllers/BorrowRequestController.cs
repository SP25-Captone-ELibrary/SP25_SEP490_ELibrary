using System.Security.Claims;
using FPTU_ELibrary.API.Extensions;
using FPTU_ELibrary.API.Payloads;
using FPTU_ELibrary.API.Payloads.Requests;
using FPTU_ELibrary.API.Payloads.Requests.Borrow;
using FPTU_ELibrary.Application.Configurations;
using FPTU_ELibrary.Application.Dtos.Borrows;
using FPTU_ELibrary.Application.Services.IServices;
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
    private readonly IBorrowRequestResourceService<BorrowRequestResourceDto> _borrowReqResourceSvc;

    public BorrowRequestController(
        IBorrowRequestService<BorrowRequestDto> borrowReqSvc,
        IBorrowRequestResourceService<BorrowRequestResourceDto> borrowReqResourceSvc,
        IOptionsMonitor<AppSettings> monitor)
    {
        _borrowReqSvc = borrowReqSvc;
        _borrowReqResourceSvc = borrowReqResourceSvc;
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
        return Ok(await _borrowReqSvc.GetByIdAsync(id, email: null, userId: null));
    }

    [Authorize]
    [HttpGet(APIRoute.BorrowRequest.CheckExistBarcode, Name = nameof(CheckExistBarcodeInRequestAsync))]
    public async Task<IActionResult> CheckExistBarcodeInRequestAsync([FromRoute] int id, [FromQuery] string barcode)
    {
        return Ok(await _borrowReqSvc.CheckExistBarcodeInRequestAsync(id: id, barcode: barcode));
    }

    [Authorize]
    [HttpPatch(APIRoute.BorrowRequest.CancelSpecificItemManagement,
        Name = nameof(CancelSpecificBorrowRequestDetailManagementAsync))]
    public async Task<IActionResult> CancelSpecificBorrowRequestDetailManagementAsync(
        [FromRoute] int id,
        [FromRoute] int libraryItemId,
        [FromQuery] Guid libraryCardId)
    {
        return Ok(await _borrowReqSvc.CancelSpecificItemManagementAsync(libraryCardId: libraryCardId, id: id,
            libraryItemId: libraryItemId));
    }

    [Authorize]
    [HttpPatch(APIRoute.BorrowRequest.CancelSpecificDigitalManagement, Name = nameof(CancelSpecificDigitalBorrowRequestManagementAsync))]
    public async Task<IActionResult> CancelSpecificDigitalBorrowRequestManagementAsync([FromRoute] int id,
        [FromRoute] int resourceId,
        [FromQuery] Guid libraryCardId)
    {
        return Ok(await _borrowReqSvc.CancelSpecificDigitalManagementAsync(
            libraryCardId: libraryCardId,
            id: id,
            resourceId: resourceId));
    }
    
    [Authorize]
    [HttpPatch(APIRoute.BorrowRequest.CancelManagement, Name = nameof(CancelBorrowRequestManagementAsync))]
    public async Task<IActionResult> CancelBorrowRequestManagementAsync([FromRoute] int id,
        [FromQuery] Guid libraryCardId,
        [FromQuery] bool isConfirmed = false,
        [FromQuery] string? cancellationReason = null)
    {
        var email = HttpContext.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
        return Ok(await _borrowReqSvc.CancelManagementAsync(
            libraryCardId: libraryCardId,
            id: id,
            isConfirmed: isConfirmed,
            cancellationReason: cancellationReason));
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
            reservationItemIds: req.ReservationItemIds,
            resourceIds: req.ResourceIds));
    }

    [Authorize]
    [HttpPost(APIRoute.BorrowRequest.AddItemToRequest, Name = nameof(AddLibraryItemToRequestAsync))]
    public async Task<IActionResult> AddLibraryItemToRequestAsync([FromRoute] int id, [FromQuery] int libraryItemId)
    {
        var email = HttpContext.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
        return Ok(await _borrowReqSvc.AddItemAsync(
            email: email ?? string.Empty,
            id: id,
            libraryItemId: libraryItemId));
    }

    [Authorize]
    [HttpPatch(APIRoute.BorrowRequest.Cancel, Name = nameof(CancelBorrowRequestAsync))]
    public async Task<IActionResult> CancelBorrowRequestAsync([FromRoute] int id,
        [FromQuery] bool isConfirmed = false,
        [FromQuery] string? cancellationReason = null)
    {
        var email = HttpContext.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
        return Ok(await _borrowReqSvc.CancelAsync(
            email: email ?? string.Empty,
            id: id,
            isConfirmed: isConfirmed,
            cancellationReason: cancellationReason));
    }

    [Authorize]
    [HttpPatch(APIRoute.BorrowRequest.CancelSpecificItem, Name = nameof(CancelSpecificBorrowRequestDetailAsync))]
    public async Task<IActionResult> CancelSpecificBorrowRequestDetailAsync([FromRoute] int id,
        [FromRoute] int libraryItemId)
    {
        var email = HttpContext.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
        return Ok(await _borrowReqSvc.CancelSpecificItemAsync(
            email: email ?? string.Empty,
            id: id,
            libraryItemId: libraryItemId));
    }
    
    [Authorize]
    [HttpPatch(APIRoute.BorrowRequest.CancelSpecificDigital, Name = nameof(CancelSpecificDigitalBorrowRequestAsync))]
    public async Task<IActionResult> CancelSpecificDigitalBorrowRequestAsync([FromRoute] int id,
        [FromRoute] int resourceId)
    {
        var email = HttpContext.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
        return Ok(await _borrowReqSvc.CancelSpecificDigitalAsync(
            email: email ?? string.Empty,
            id: id,
            resourceId: resourceId));
    }

    [Authorize]
    [HttpGet(APIRoute.BorrowRequest.ConfirmCreateTransaction, Name = nameof(ConfirmCreateTransactionForRequestResourceAsync))]
    public async Task<IActionResult> ConfirmCreateTransactionForRequestResourceAsync([FromRoute] int id)
    {
        var email = HttpContext.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
        return Ok(await _borrowReqResourceSvc.GetAllByRequestIdToConfirmCreateTransactionAsync(
            email: email ?? string.Empty,
            borrowRequestId: id));
    }
}