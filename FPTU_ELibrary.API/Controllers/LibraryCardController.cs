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
    private readonly IInvoiceService<InvoiceDto> _invoiceSvc;
    private readonly IBorrowRequestService<BorrowRequestDto> _borrowReqSvc;
    private readonly IBorrowRecordService<BorrowRecordDto> _borrowRecSvc;
    private readonly ITransactionService<TransactionDto> _transactionSvc;
    private readonly INotificationService<NotificationDto> _notificationSvc;
    private readonly IDigitalBorrowService<DigitalBorrowDto> _digitalBorrowSvc;
    private readonly IReservationQueueService<ReservationQueueDto> _reservationQueueSvc;

    public LibraryCardController(
        ILogger logger,
        IUserService<UserDto> userSvc,
        IDigitalBorrowService<DigitalBorrowDto> digitalBorrowSvc,
        ILibraryCardService<LibraryCardDto> cardSvc,
        IBorrowRequestService<BorrowRequestDto> borrowReqSvc,
        IBorrowRecordService<BorrowRecordDto> borrowRecSvc,
        IReservationQueueService<ReservationQueueDto> reservationQueueSvc,
        IInvoiceService<InvoiceDto> invoiceSvc,
        ITransactionService<TransactionDto> transactionSvc,
        INotificationService<NotificationDto> notificationSvc,
        IOptionsMonitor<WebTokenSettings> monitor,
        IOptionsMonitor<AppSettings> monitor1)
    {
        _logger = logger;
        _userSvc = userSvc;
        _cardSvc = cardSvc;
        _invoiceSvc = invoiceSvc;
        _digitalBorrowSvc = digitalBorrowSvc;
        _borrowReqSvc = borrowReqSvc;
        _borrowRecSvc = borrowRecSvc;
        _transactionSvc = transactionSvc;
        _reservationQueueSvc = reservationQueueSvc;
        _notificationSvc = notificationSvc;
        _webTokenSettings = monitor.CurrentValue;
        _appSettings = monitor1.CurrentValue;
    }

    #region Library Card Management
    [Authorize]
    [HttpPost(APIRoute.LibraryCard.AddCard, Name = nameof(AddLibraryCardAsync))]
    public async Task<IActionResult> AddLibraryCardAsync([FromBody] AddLibraryCardAsync req)
    {
        var processedByEmail = HttpContext.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
        return Ok(await _userSvc.RegisterLibraryCardByEmployeeAsync(
            processedByEmail: processedByEmail ?? string.Empty, 
            userId: req.UserId,
            userWithCard: req.ToUserWithLibraryCardDto()));
    }
    
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
        return Ok(await _cardSvc.GetByIdAsync(id));
    }
    
    [Authorize]
    [HttpPatch(APIRoute.LibraryCard.SuspendCard, Name = nameof(SuspendLibraryCardAsync))]
    public async Task<IActionResult> SuspendLibraryCardAsync([FromRoute] Guid id, 
        [FromQuery] DateTime suspensionEndDate)
    {
        return Ok(await _cardSvc.SuspendCardAsync(libraryCardId: id, suspensionEndDate: suspensionEndDate));
    }
    
    [Authorize]
    [HttpPatch(APIRoute.LibraryCard.UnsuspendCard, Name = nameof(UnsuspendLibraryCardAsync))]
    public async Task<IActionResult> UnsuspendLibraryCardAsync([FromRoute] Guid id)
    {
        return Ok(await _cardSvc.UnsuspendCardAsync(libraryCardId: id));
    }
    
    #endregion

    #region Library Card Holder Management
    [Authorize]
    [HttpPost(APIRoute.LibraryCard.Create, Name = nameof(CreateLibraryCardHolderAsync))]
    public async Task<IActionResult> CreateLibraryCardHolderAsync([FromBody] CreateLibraryCardHolderRequest req)
    {
        return Ok(await _userSvc.CreateLibraryCardHolderAsync(req.ToLibraryCardHolderDto()));
    }

    [Authorize]
    [HttpPut(APIRoute.LibraryCard.UpdateHolder, Name = nameof(UpdateLibraryCardHolderAsync))]
    public async Task<IActionResult> UpdateLibraryCardHolderAsync([FromRoute] Guid userId, [FromBody] UpdateLibraryCardHolderRequest req)
    {
        return Ok(await _userSvc.UpdateLibraryCardHolderAsync(userId: userId, dto: req.ToLibraryCardHolderDto()));
    }
    
    [Authorize]
    [HttpGet(APIRoute.LibraryCard.GetAllCardHolders, Name = nameof(GetAllLibraryCardHoldersAsync))]
    public async Task<IActionResult> GetAllLibraryCardHoldersAsync([FromQuery] LibraryCardHolderSpecParams specParams)
    {
        return Ok(await _userSvc.GetAllLibraryCardHolderAsync(
            new LibraryCardHolderSpecification(
                specParams: specParams,
                pageIndex: specParams.PageIndex ?? 1,
                pageSize: specParams.PageSize ?? _appSettings.PageSize)));
    }

    [Authorize]
    [HttpGet(APIRoute.LibraryCard.GetCardHolderById, Name = nameof(GetCardHolderByUserIdAsync))]
    public async Task<IActionResult> GetCardHolderByUserIdAsync([FromRoute] Guid userId)
    {
        return Ok(await _userSvc.GetLibraryCardHolderByIdAsync(userId: userId));
    }
    
    [Authorize]
    [HttpGet(APIRoute.LibraryCard.GetAllCardHolderBorrowRequest, Name = nameof(GetAllCardHolderBorrowRequestAsync))]
    public async Task<IActionResult> GetAllCardHolderBorrowRequestAsync([FromRoute] Guid userId,
        [FromQuery] int? pageIndex, [FromQuery] int? pageSize)
    {
        return Ok(await _borrowReqSvc.GetAllByUserIdAsync(
            userId: userId,
            pageIndex: pageIndex ?? 1,
            pageSize: pageSize ?? _appSettings.PageSize));
    }
    
    [Authorize]
    [HttpGet(APIRoute.LibraryCard.GetAllCardHolderBorrowRecord, Name = nameof(GetAllCardHolderBorrowRecordAsync))]
    public async Task<IActionResult> GetAllCardHolderBorrowRecordAsync([FromRoute] Guid userId,
        [FromQuery] int? pageIndex, [FromQuery] int? pageSize)
    {
        return Ok(await _borrowRecSvc.GetAllByUserIdAsync(
            userId: userId,
            pageIndex: pageIndex ?? 1,
            pageSize: pageSize ?? _appSettings.PageSize));
    }
    
    [Authorize]
    [HttpGet(APIRoute.LibraryCard.GetAllCardHolderDigitalBorrow, Name = nameof(GetAllCardHolderDigitalBorrowAsync))]
    public async Task<IActionResult> GetAllCardHolderDigitalBorrowAsync([FromRoute] Guid userId,
        [FromQuery] int? pageIndex, [FromQuery] int? pageSize)
    {
        return Ok(await _digitalBorrowSvc.GetAllByUserIdAsync(
            userId: userId,
            pageIndex: pageIndex ?? 1,
            pageSize: pageSize ?? _appSettings.PageSize));
    }
    
    [Authorize]
    [HttpGet(APIRoute.LibraryCard.GetAllCardHolderReservation, Name = nameof(GetAllCardHolderReservationAsync))]
    public async Task<IActionResult> GetAllCardHolderReservationAsync([FromRoute] Guid userId,
        [FromQuery] int? pageIndex, [FromQuery] int? pageSize)
    {
        return Ok(await _reservationQueueSvc.GetAllByUserIdAsync(
            userId: userId,
            pageIndex: pageIndex ?? 1,
            pageSize: pageSize ?? _appSettings.PageSize));
    }
    
    [Authorize]
    [HttpGet(APIRoute.LibraryCard.GetAllCardHolderInvoice, Name = nameof(GetAllCardHolderInvoiceAsync))]
    public async Task<IActionResult> GetAllCardHolderInvoiceAsync([FromRoute] Guid userId,
        [FromQuery] int? pageIndex, [FromQuery] int? pageSize)
    {
        return Ok(await _invoiceSvc.GetAllByUserIdAsync(
            userId: userId,
            pageIndex: pageIndex ?? 1,
            pageSize: pageSize ?? _appSettings.PageSize));
    }
    
    [Authorize]
    [HttpGet(APIRoute.LibraryCard.GetAllCardHolderTransaction, Name = nameof(GetAllCardHolderTransactionAsync))]
    public async Task<IActionResult> GetAllCardHolderTransactionAsync([FromRoute] Guid userId,
        [FromQuery] int? pageIndex, [FromQuery] int? pageSize)
    {
        return Ok(await _transactionSvc.GetAllByUserIdAsync(
            userId: userId,
            pageIndex: pageIndex ?? 1,
            pageSize: pageSize ?? _appSettings.PageSize));
    }
    
    [Authorize]
    [HttpGet(APIRoute.LibraryCard.GetAllCardHolderNotification, Name = nameof(GetAllCardHolderNotificationAsync))]
    public async Task<IActionResult> GetAllCardHolderNotificationAsync([FromRoute] Guid userId,
        [FromQuery] int? pageIndex, [FromQuery] int? pageSize)
    {
        return Ok(await _notificationSvc.GetAllByUserIdAsync(
            userId: userId,
            pageIndex: pageIndex ?? 1,
            pageSize: pageSize ?? _appSettings.PageSize));
    }

    [Authorize]
    [HttpPut(APIRoute.LibraryCard.ArchiveCard, Name = nameof(ArchiveLibraryCardAsync))]
    public async Task<IActionResult> ArchiveLibraryCardAsync([FromRoute] Guid userId, 
        [FromBody] ArchiveLibraryCardRequest req)
    {
        return Ok(await _cardSvc.ArchiveCardAsync(userId: userId, libraryCardId: req.LibraryCardId, archiveReason: req.ArchiveReason));
    }
    
    #endregion
    
    // TODO: Remove this function
    [Authorize]
    [HttpGet("generate-code")]
    public async Task<IActionResult> GenerateCodeAsync()
    {
        var email = HttpContext.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
        return Ok(await new PaymentUtils(_logger).GenerateTransactionTokenAsync(
            email: email ?? string.Empty, "CODE123", new DateTime(2025,02,09), _webTokenSettings));
    }

    [Authorize]
    [HttpGet(APIRoute.LibraryCard.CheckCardExtension, Name = nameof(CheckCardExtensionAsync))]
    public async Task<IActionResult> CheckCardExtensionAsync([FromRoute] Guid id)
    {
        return Ok(await _cardSvc.CheckCardExtensionAsync(id));
    }

    [Authorize]
    [HttpGet(APIRoute.LibraryCard.GetByBarcode, Name = nameof(GetLibraryCardHolderByBarcodeAsync))]
    public async Task<IActionResult> GetLibraryCardHolderByBarcodeAsync([FromQuery] string barcode)
    {
        return Ok(await _userSvc.GetLibraryCardHolderByBarcodeAsync(barcode: barcode));
    }
    
    [Authorize]
    [HttpPost(APIRoute.LibraryCard.Register, Name = nameof(RegisterLibraryCardOnlineAsync))]
    public async Task<IActionResult> RegisterLibraryCardOnlineAsync([FromBody] RegisterLibraryCardOnlineRequest req)
    {
        var email = HttpContext.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
        return Ok(await _userSvc.RegisterLibraryCardAsync(
            email: email ?? string.Empty, 
            userWithCard: req.ToUserWithLibraryCardDto()));
    }

    [Authorize]
    [HttpGet(APIRoute.LibraryCard.GetCardHolderDetailByEmail, Name = nameof(GetCardHolderDetailByEmailAsync))]
    public async Task<IActionResult> GetCardHolderDetailByEmailAsync()
    {
        var email = HttpContext.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
        return Ok(await _userSvc.GetLibraryCardHolderDetailByEmailAsync(email: email ?? string.Empty));
    }
    
    [Authorize]
    [HttpGet(APIRoute.LibraryCard.GetAllCardHolderBorrowRequestByEmail, Name = nameof(GetAllCardHolderBorrowRequestByEmailAsync))]
    public async Task<IActionResult> GetAllCardHolderBorrowRequestByEmailAsync(
        [FromQuery] int? pageIndex, [FromQuery] int? pageSize)
    {
        var email = HttpContext.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
        return Ok(await _borrowReqSvc.GetAllByEmailAsync(
            email: email ?? string.Empty,
            pageIndex: pageIndex ?? 1,
            pageSize: pageSize ?? _appSettings.PageSize));
    }
    
    [Authorize]
    [HttpGet(APIRoute.LibraryCard.GetAllCardHolderBorrowRecordByEmail, Name = nameof(GetAllCardHolderBorrowRecordByEmailAsync))]
    public async Task<IActionResult> GetAllCardHolderBorrowRecordByEmailAsync(
        [FromQuery] int? pageIndex, [FromQuery] int? pageSize)
    {
        var email = HttpContext.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
        return Ok(await _borrowRecSvc.GetAllByEmailAsync(
            email: email ?? string.Empty,
            pageIndex: pageIndex ?? 1,
            pageSize: pageSize ?? _appSettings.PageSize));
    }
    
    [Authorize]
    [HttpGet(APIRoute.LibraryCard.GetAllCardHolderDigitalBorrowByEmail, Name = nameof(GetAllCardHolderDigitalBorrowByEmailAsync))]
    public async Task<IActionResult> GetAllCardHolderDigitalBorrowByEmailAsync(
        [FromQuery] int? pageIndex, [FromQuery] int? pageSize)
    {
        var email = HttpContext.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
        return Ok(await _digitalBorrowSvc.GetAllByEmailAsync(
            email: email ?? string.Empty,
            pageIndex: pageIndex ?? 1,
            pageSize: pageSize ?? _appSettings.PageSize));
    }
    
    [Authorize]
    [HttpGet(APIRoute.LibraryCard.GetAllCardHolderReservationByEmail, Name = nameof(GetAllCardHolderReservationByEmailAsync))]
    public async Task<IActionResult> GetAllCardHolderReservationByEmailAsync(
        [FromQuery] int? pageIndex, [FromQuery] int? pageSize)
    {
        var email = HttpContext.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
        return Ok(await _reservationQueueSvc.GetAllByEmailAsync(
            email: email ?? string.Empty,
            pageIndex: pageIndex ?? 1,
            pageSize: pageSize ?? _appSettings.PageSize));
    }
    
    [Authorize]
    [HttpGet(APIRoute.LibraryCard.GetAllCardHolderInvoiceByEmail, Name = nameof(GetAllCardHolderInvoiceByEmailAsync))]
    public async Task<IActionResult> GetAllCardHolderInvoiceByEmailAsync(
        [FromQuery] int? pageIndex, [FromQuery] int? pageSize)
    {
        var email = HttpContext.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
        return Ok(await _invoiceSvc.GetAllByEmailAsync(
            email: email ?? string.Empty,
            pageIndex: pageIndex ?? 1,
            pageSize: pageSize ?? _appSettings.PageSize));
    }
    
    [Authorize]
    [HttpGet(APIRoute.LibraryCard.GetAllCardHolderTransactionByEmail, Name = nameof(GetAllCardHolderTransactionByEmailAsync))]
    public async Task<IActionResult> GetAllCardHolderTransactionByEmailAsync(
        [FromQuery] int? pageIndex, [FromQuery] int? pageSize)
    {
        var email = HttpContext.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
        return Ok(await _transactionSvc.GetAllByEmailAsync(
            email: email ?? string.Empty,
            pageIndex: pageIndex ?? 1,
            pageSize: pageSize ?? _appSettings.PageSize));
    }
    
    [Authorize]
    [HttpGet(APIRoute.LibraryCard.GetAllCardHolderNotificationByEmail, Name = nameof(GetAllCardHolderNotificationByEmailAsync))]
    public async Task<IActionResult> GetAllCardHolderNotificationByEmailAsync(
        [FromQuery] int? pageIndex, [FromQuery] int? pageSize)
    {
        var email = HttpContext.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
        return Ok(await _notificationSvc.GetAllByEmailAsync(
            email: email ?? string.Empty,
            pageIndex: pageIndex ?? 1,
            pageSize: pageSize ?? _appSettings.PageSize));
    }
    
    // TODO: Remove this function
    [Authorize]
    [HttpPatch(APIRoute.LibraryCard.ConfirmRegister, Name = nameof(ConfirmRegisterLibraryCardAsync))]
    public async Task<IActionResult> ConfirmRegisterLibraryCardAsync([FromBody] ConfirmLibraryCardRequest req)
    {
        var email = HttpContext.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
        return Ok(await _cardSvc.ConfirmRegisterAsync(
            libraryCardId: req.LibraryCardId, 
            transactionToken: req.TransactionToken));
    }
    
    // TODO: Remove this function
    [Authorize]
    [HttpPatch(APIRoute.LibraryCard.ConfirmExtend, Name = nameof(ConfirmExtendLibraryCardAsync))]
    public async Task<IActionResult> ConfirmExtendLibraryCardAsync([FromBody] ConfirmLibraryCardRequest req)
    {
        var email = HttpContext.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
        return Ok(await _cardSvc.ConfirmExtendCardAsync(
            libraryCardId: req.LibraryCardId, 
            transactionToken: req.TransactionToken));
    }
}