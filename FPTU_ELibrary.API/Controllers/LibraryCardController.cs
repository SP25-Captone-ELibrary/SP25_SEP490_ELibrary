using System.Security.Claims;
using FPTU_ELibrary.API.Extensions;
using FPTU_ELibrary.API.Payloads;
using FPTU_ELibrary.API.Payloads.Requests.LibraryCard;
using FPTU_ELibrary.Application.Configurations;
using FPTU_ELibrary.Application.Dtos;
using FPTU_ELibrary.Application.Dtos.Borrows;
using FPTU_ELibrary.Application.Dtos.LibraryCard;
using FPTU_ELibrary.Application.Dtos.LibraryItems;
using FPTU_ELibrary.Application.Dtos.Notifications;
using FPTU_ELibrary.Application.Dtos.Payments;
using FPTU_ELibrary.Application.Utils;
using FPTU_ELibrary.Domain.Interfaces.Services;
using FPTU_ELibrary.Domain.Interfaces.Services.Base;
using FPTU_ELibrary.Domain.Specifications;
using FPTU_ELibrary.Domain.Specifications.Params;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Nest;
using ILogger = Serilog.ILogger;

namespace FPTU_ELibrary.API.Controllers;

[ApiController]
public class LibraryCardController : ControllerBase
{
    private readonly ILogger _logger;
    private readonly AppSettings _appSettings;
    private readonly WebTokenSettings _webTokenSettings;
    
    private readonly IUserService<UserDto> _userSvc;
    private readonly ILibraryCardService<LibraryCardDto> _cardSvc;

    public LibraryCardController(
        IUserService<UserDto> userSvc,
        ILogger logger,
        ILibraryCardService<LibraryCardDto> cardSvc,
        IOptionsMonitor<WebTokenSettings> monitor,
        IOptionsMonitor<AppSettings> monitor1)
    {
        _logger = logger;
        _cardSvc = cardSvc;
        _userSvc = userSvc;
        _webTokenSettings = monitor.CurrentValue;
        _appSettings = monitor1.CurrentValue;
    }

    #region Management
    [Authorize]
    [HttpPut(APIRoute.LibraryCard.UpdateCard, Name = nameof(UpdateLibraryCardAsync))]
    public async Task<IActionResult> UpdateLibraryCardAsync([FromRoute] Guid id, [FromBody] UpdateLibraryCardRequest req)
    {
        return Ok(await _cardSvc.UpdateAsync(id: id, dto: req.ToLibraryCardDto()));
    }

    [Authorize]
    [HttpDelete(APIRoute.LibraryCard.DeleteCard, Name = nameof(DeleteLibraryCardAsync))]
    public async Task<IActionResult> DeleteLibraryCardAsync([FromRoute] Guid id)
    {
        return Ok(await _cardSvc.DeleteAsync(id: id));
    }
    
    [Authorize]
    [HttpGet(APIRoute.LibraryCard.GetAllCard, Name = nameof(GetAllLibraryCardAsync))]
    public async Task<IActionResult> GetAllLibraryCardAsync([FromQuery] LibraryCardSpecParams specParams)
    {
        return Ok(await _cardSvc.GetAllWithSpecAsync(new LibraryCardSpecification(
            specParams: specParams,
            pageIndex: specParams.PageIndex ?? 1,
            pageSize: specParams.PageSize ?? _appSettings.PageSize)));
    }

    [Authorize]
    [HttpGet(APIRoute.LibraryCard.GetCardById, Name = nameof(GetLibraryCardByIdAsync))]
    public async Task<IActionResult> GetLibraryCardByIdAsync([FromRoute] Guid id)
    {
        return Ok(await _cardSvc.GetDetailAsync(id));
    }

    [Authorize]
    [HttpPatch(APIRoute.LibraryCard.Confirm, Name = nameof(ConfirmLibraryCardAsync))]
    public async Task<IActionResult> ConfirmLibraryCardAsync([FromRoute] Guid id)
    {
        return Ok(await _cardSvc.ConfirmCardAsync(libraryCardId: id));
    }
    
    [Authorize]
    [HttpPatch(APIRoute.LibraryCard.Reject, Name = nameof(RejectLibraryCardAsync))]
    public async Task<IActionResult> RejectLibraryCardAsync([FromRoute] Guid id, 
        [FromQuery] string rejectReason)
    {
        return Ok(await _cardSvc.RejectCardAsync(libraryCardId: id, rejectReason: rejectReason));
    }
    
    [Authorize]
    [HttpPatch(APIRoute.LibraryCard.ExtendCard, Name = nameof(ExtendLibraryCardByEmployeeAsync))]
    public async Task<IActionResult> ExtendLibraryCardByEmployeeAsync([FromRoute] Guid id, [FromBody] ExtendLibraryCardRequest req)
    {
        var email = HttpContext.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
        return Ok(await _cardSvc.ExtendCardByEmployeeAsync(
            processedByEmail: email ?? string.Empty, 
            libraryCardId: id,
            transactionMethod: req.TransactionMethod,
            libraryCardPackageId: req.LibraryCardPackageId,
            paymentMethodId: req.PaymentMethodId));
    }
    
    [Authorize]
    [HttpPatch(APIRoute.LibraryCard.ExtendBorrowAmount, Name = nameof(ExtendLibraryCardBorrowAmountAsync))]
    public async Task<IActionResult> ExtendLibraryCardBorrowAmountAsync([FromRoute] Guid id,
        [FromQuery] int maxItemOnceTime, [FromQuery] string reason)
    {
        return Ok(await _cardSvc.ExtendBorrowAmountAsync(
            libraryCardId: id,
            maxItemOnceTime: maxItemOnceTime,
            reason: reason));
    }
    
    [Authorize]
    [HttpPatch(APIRoute.LibraryCard.SuspendCard, Name = nameof(SuspendLibraryCardAsync))]
    public async Task<IActionResult> SuspendLibraryCardAsync([FromRoute] Guid id, 
        [FromQuery] DateTime suspensionEndDate, [FromQuery] string reason)
    {
        return Ok(await _cardSvc.SuspendCardAsync(
            libraryCardId: id, 
            suspensionEndDate: suspensionEndDate,
            reason: reason));
    }
    
    [Authorize]
    [HttpPatch(APIRoute.LibraryCard.UnsuspendCard, Name = nameof(UnsuspendLibraryCardAsync))]
    public async Task<IActionResult> UnsuspendLibraryCardAsync([FromRoute] Guid id)
    {
        return Ok(await _cardSvc.UnsuspendCardAsync(libraryCardId: id));
    }
    
    [Authorize]
    [HttpPut(APIRoute.LibraryCard.ArchiveCard, Name = nameof(ArchiveLibraryCardAsync))]
    public async Task<IActionResult> ArchiveLibraryCardAsync([FromRoute] Guid userId, 
        [FromBody] ArchiveLibraryCardRequest req)
    {
        return Ok(await _cardSvc.ArchiveCardAsync(userId: userId, libraryCardId: req.LibraryCardId, archiveReason: req.ArchiveReason));
    }
    

    #endregion
    
    [Authorize]
    [HttpPost(APIRoute.LibraryCard.Register, Name = nameof(RegisterLibraryCardOnlineAsync))]
    public async Task<IActionResult> RegisterLibraryCardOnlineAsync([FromBody] RegisterLibraryCardOnlineRequest req)
    {
        var email = HttpContext.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
        return Ok(await _cardSvc.RegisterCardAsync(email: email ?? string.Empty, dto: req.ToUserWithLibraryCardDto()));
    }
    
    [Authorize]
    [HttpPatch(APIRoute.LibraryCard.SendReConfirm, Name = nameof(SendReConfirmLibraryCardAsync))]
    public async Task<IActionResult> SendReConfirmLibraryCardAsync([FromRoute] Guid id)
    {
        var email = HttpContext.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
        return Ok(await _cardSvc.SendRequireToConfirmCardAsync(userEmail: email ?? string.Empty));
    }
    
    [Authorize]
    [HttpGet(APIRoute.LibraryCard.CheckCardExtension, Name = nameof(CheckCardExtensionAsync))]
    public async Task<IActionResult> CheckCardExtensionAsync([FromRoute] Guid id)
    {
        return Ok(await _cardSvc.CheckCardExtensionAsync(id));
    }
}