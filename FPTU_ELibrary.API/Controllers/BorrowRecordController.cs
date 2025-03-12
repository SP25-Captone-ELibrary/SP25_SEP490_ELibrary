using System.Security.Claims;
using FPTU_ELibrary.API.Extensions;
using FPTU_ELibrary.API.Payloads;
using FPTU_ELibrary.API.Payloads.Requests.Borrow;
using FPTU_ELibrary.API.Payloads.Requests.LibraryCard;
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
public class BorrowRecordController : ControllerBase
{
    private readonly IBorrowRecordService<BorrowRecordDto> _borrowRecSvc;
    private readonly AppSettings _appSettings;

    public BorrowRecordController(
        IBorrowRecordService<BorrowRecordDto> borrowRecSvc,
        IOptionsMonitor<AppSettings> monitor)
    {
        _borrowRecSvc = borrowRecSvc;
        _appSettings = monitor.CurrentValue;
    }
    
    #region Management
    [Authorize]
    [HttpGet(APIRoute.BorrowRecord.GetAll, Name = nameof(GetAllBorrowRecordAsync))]
    public async Task<IActionResult> GetAllBorrowRecordAsync([FromQuery] BorrowRecordSpecParams specParams)
    {
        return Ok(await _borrowRecSvc.GetAllWithSpecAsync(new BorrowRecordSpecification(
            specParams: specParams,
            pageIndex: specParams.PageIndex ?? 1,
            pageSize: specParams.PageSize ?? _appSettings.PageSize)));
    }
    
    [Authorize]
    [HttpGet(APIRoute.BorrowRecord.GetById, Name = nameof(GetBorrowRecordByIdAsync))]
    public async Task<IActionResult> GetBorrowRecordByIdAsync([FromRoute] int id)
    {
        return Ok(await _borrowRecSvc.GetByIdAsync(id: id));
    }
    
    [Authorize]
    [HttpPost(APIRoute.BorrowRecord.ProcessRequest, Name = nameof(ProcessRequestToBorrowRecordAsync))]
    public async Task<IActionResult> ProcessRequestToBorrowRecordAsync([FromBody] ProcessToBorrowRecordRequest req)
    {
        var email = HttpContext.User.Claims.FirstOrDefault(claim => claim.Type == ClaimTypes.Email)?.Value;
        return Ok(await _borrowRecSvc.ProcessRequestToBorrowRecordAsync(
            processedByEmail: email ?? string.Empty, dto: req.ToBorrowRecordDto()));
    }

    [Authorize]
    [HttpPost(APIRoute.BorrowRecord.Create, Name = nameof(CreateBorrowRecordAsync))]
    public async Task<IActionResult> CreateBorrowRecordAsync([FromBody] CreateBorrowRecordRequest req)
    {
        var email = HttpContext.User.Claims.FirstOrDefault(claim => claim.Type == ClaimTypes.Email)?.Value;
        return Ok(await _borrowRecSvc.CreateAsync(processedByEmail: email ?? string.Empty, dto: req.ToBorrowRecordDto()));
    }
    #endregion

    [Authorize]
    [HttpPost(APIRoute.BorrowRecord.SelfCheckout, Name = nameof(SelfCheckoutBorrowAsync))]
    public async Task<IActionResult> SelfCheckoutBorrowAsync([FromBody] SelfCheckoutBorrowRequest req)
    {
        return Ok(await _borrowRecSvc.SelfCheckoutAsync(
            libraryCardId: req.LibraryCardId, dto: req.ToBorrowRecordDto()));
    }
    
    [Authorize]
    [HttpPut(APIRoute.BorrowRecord.Extend, Name = nameof(ExtendBorrowRecordAsync))]
    public async Task<IActionResult> ExtendBorrowRecordAsync([FromRoute] int id, [FromBody] ExtendBorrowRecordRequest req)
    {
        var email = HttpContext.User.Claims.FirstOrDefault(claim => claim.Type == ClaimTypes.Email)?.Value;
        return Ok(await _borrowRecSvc.ExtendAsync(
            email: email ?? string.Empty, 
            borrowRecordId: id,
            borrowRecordDetailIds: req.BorrowRecordDetailIds));   
    }
}