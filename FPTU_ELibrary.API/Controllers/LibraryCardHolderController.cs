using System.Security.Claims;
using FPTU_ELibrary.API.Extensions;
using FPTU_ELibrary.API.Payloads;
using FPTU_ELibrary.API.Payloads.Requests;
using FPTU_ELibrary.API.Payloads.Requests.LibraryCard;
using FPTU_ELibrary.Application.Configurations;
using FPTU_ELibrary.Application.Dtos;
using FPTU_ELibrary.Application.Dtos.Borrows;
using FPTU_ELibrary.Application.Dtos.LibraryCard;
using FPTU_ELibrary.Application.Dtos.LibraryItems;
using FPTU_ELibrary.Application.Dtos.Notifications;
using FPTU_ELibrary.Application.Dtos.Payments;
using FPTU_ELibrary.Domain.Common.Enums;
using FPTU_ELibrary.Domain.Interfaces.Services;
using FPTU_ELibrary.Domain.Interfaces.Services.Base;
using FPTU_ELibrary.Domain.Specifications;
using FPTU_ELibrary.Domain.Specifications.Params;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace FPTU_ELibrary.API.Controllers;

[ApiController]
public class LibraryCardHolderController : ControllerBase
{
    private readonly AppSettings _appSettings;

    private readonly IUserService<UserDto> _userSvc;
    private readonly IInvoiceService<InvoiceDto> _invoiceSvc;
    private readonly IBorrowRequestService<BorrowRequestDto> _borrowReqSvc;
    private readonly IBorrowRecordService<BorrowRecordDto> _borrowRecSvc;
    private readonly ITransactionService<TransactionDto> _transactionSvc;
    private readonly INotificationService<NotificationDto> _notificationSvc;
    private readonly IDigitalBorrowService<DigitalBorrowDto> _digitalBorrowSvc;
    private readonly IReservationQueueService<ReservationQueueDto> _reservationQueueSvc;

    public LibraryCardHolderController(
        IUserService<UserDto> userSvc,
        IDigitalBorrowService<DigitalBorrowDto> digitalBorrowSvc,
        IBorrowRequestService<BorrowRequestDto> borrowReqSvc,
        IBorrowRecordService<BorrowRecordDto> borrowRecSvc,
        IReservationQueueService<ReservationQueueDto> reservationQueueSvc,
        IInvoiceService<InvoiceDto> invoiceSvc,
        ITransactionService<TransactionDto> transactionSvc,
        INotificationService<NotificationDto> notificationSvc,
        IOptionsMonitor<AppSettings> monitor1)
    {
        _userSvc = userSvc;
        _invoiceSvc = invoiceSvc;
        _digitalBorrowSvc = digitalBorrowSvc;
        _borrowReqSvc = borrowReqSvc;
        _borrowRecSvc = borrowRecSvc;
        _transactionSvc = transactionSvc;
        _reservationQueueSvc = reservationQueueSvc;
        _notificationSvc = notificationSvc;
        _appSettings = monitor1.CurrentValue;
    }

    #region Management

    [Authorize]
    [HttpPost(APIRoute.LibraryCardHolder.Create, Name = nameof(CreateLibraryCardHolderAsync))]
    public async Task<IActionResult> CreateLibraryCardHolderAsync([FromBody] CreateLibraryCardHolderRequest req)
    {
        return Ok(await _userSvc.CreateLibraryCardHolderAsync(dto: req.ToLibraryCardHolderDto()));
    }

    [Authorize]
    [HttpPost(APIRoute.LibraryCardHolder.AddCard, Name = nameof(AddLibraryCardAsync))]
    public async Task<IActionResult> AddLibraryCardAsync([FromBody] AddLibraryCardAsync req)
    {
        var processedByEmail = HttpContext.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
        return Ok(await _userSvc.RegisterLibraryCardByEmployeeAsync(
            processedByEmail: processedByEmail ?? string.Empty,
            userId: req.UserId,
            userWithCard: req.ToUserWithLibraryCardDto(),
            transactionToken: req.TransactionToken,
            libraryCardPackageId: req.LibraryCardPackageId
        ));
    }

    [Authorize]
    [HttpPost(APIRoute.LibraryCardHolder.Import, Name = nameof(ImportLibraryCardHolderAsync))]
    public async Task<IActionResult> ImportLibraryCardHolderAsync([FromForm] ImportLibraryCardHolderRequest req)
    {
        return Ok(await _userSvc.ImportLibraryCardHolderAsync(
            file: req.File,
            avatarImageFiles: req.AvatarImageFiles,
            scanningFields: req.ScanningFields,
            duplicateHandle: req.DuplicateHandle));
    }

    // [Authorize]
    [HttpGet(APIRoute.LibraryCardHolder.Export, Name = nameof(ExportLibraryCardHoldersAsync))]
    public async Task<IActionResult> ExportLibraryCardHoldersAsync([FromQuery] LibraryCardHolderSpecParams specParams)
    {
        var exportResult = await _userSvc.ExportLibraryCardHolderAsync(
            new LibraryCardHolderSpecification(
                specParams: specParams,
                pageIndex: specParams.PageIndex ?? 1,
                pageSize: specParams.PageSize ?? _appSettings.PageSize));

        return exportResult.Data is byte[] fileStream
            ? File(fileStream, @"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "library-card-holders.xlsx")
            : Ok(exportResult);
    }

    [Authorize]
    [HttpPut(APIRoute.LibraryCardHolder.UpdateCardHolder, Name = nameof(UpdateLibraryCardHolderAsync))]
    public async Task<IActionResult> UpdateLibraryCardHolderAsync([FromRoute] Guid userId,
        [FromBody] UpdateLibraryCardHolderRequest req)
    {
        return Ok(await _userSvc.UpdateLibraryCardHolderAsync(userId: userId, dto: req.ToLibraryCardHolderDto()));
    }

    [Authorize]
    [HttpGet(APIRoute.LibraryCardHolder.GetAllCardHolders, Name = nameof(GetAllLibraryCardHoldersAsync))]
    public async Task<IActionResult> GetAllLibraryCardHoldersAsync([FromQuery] LibraryCardHolderSpecParams specParams)
    {
        return Ok(await _userSvc.GetAllLibraryCardHolderAsync(
            new LibraryCardHolderSpecification(
                specParams: specParams,
                pageIndex: specParams.PageIndex ?? 1,
                pageSize: specParams.PageSize ?? _appSettings.PageSize)));
    }

    [Authorize]
    [HttpGet(APIRoute.LibraryCardHolder.GetCardHolderById, Name = nameof(GetCardHolderByUserIdAsync))]
    public async Task<IActionResult> GetCardHolderByUserIdAsync([FromRoute] Guid userId)
    {
        return Ok(await _userSvc.GetLibraryCardHolderByIdAsync(userId: userId));
    }

    [Authorize]
    [HttpGet(APIRoute.LibraryCardHolder.GetCardHolderBorrowRequestById,
        Name = nameof(GetCardHolderBorrowRequestByIdAsync))]
    public async Task<IActionResult> GetCardHolderBorrowRequestByIdAsync([FromRoute] Guid userId,
        [FromRoute] int requestId)
    {
        return Ok(await _borrowReqSvc.GetCardHolderBorrowRequestByIdAsync(userId: userId, id: requestId));
    }

    [Authorize]
    [HttpGet(APIRoute.LibraryCardHolder.GetCardHolderBorrowRecordById,
        Name = nameof(GetCardHolderBorrowRecordByIdAsync))]
    public async Task<IActionResult> GetCardHolderBorrowRecordByIdAsync([FromRoute] Guid userId,
        [FromRoute] int borrowRecordId)
    {
        return Ok(
            await _borrowRecSvc.GetCardHolderBorrowRecordByIdAsync(userId: userId, borrowRecordId: borrowRecordId));
    }

    [Authorize]
    [HttpGet(APIRoute.LibraryCardHolder.GetCardHolderDigitalBorrowById,
        Name = nameof(GetCardHolderDigitalBorrowByIdAsync))]
    public async Task<IActionResult> GetCardHolderDigitalBorrowByIdAsync([FromRoute] Guid userId,
        [FromRoute] int digitalBorrowId)
    {
        return Ok(await _digitalBorrowSvc.GetCardHolderDigitalBorrowByIdAsync(userId: userId,
            digitalBorrowId: digitalBorrowId));
    }

    [Authorize]
    [HttpGet(APIRoute.LibraryCardHolder.GetCardHolderInvoiceById, Name = nameof(GetCardHolderInvoiceByIdAsync))]
    public async Task<IActionResult> GetCardHolderInvoiceByIdAsync([FromRoute] Guid userId, [FromRoute] int invoiceId)
    {
        return Ok(await _invoiceSvc.GetCardHolderInvoiceByIdAsync(userId: userId, invoiceId: invoiceId));
    }

    [Authorize]
    [HttpGet(APIRoute.LibraryCardHolder.GetCardHolderTransactionById, Name = nameof(GetCardHolderTransactionByIdAsync))]
    public async Task<IActionResult> GetCardHolderTransactionByIdAsync([FromRoute] Guid userId,
        [FromRoute] int transactionId)
    {
        return Ok(await _transactionSvc.GetCardHolderTransactionByIdAsync(userId: userId,
            transactionId: transactionId));
    }

    [Authorize]
    [HttpGet(APIRoute.LibraryCardHolder.GetAllCardHolderBorrowRequest,
        Name = nameof(GetAllCardHolderBorrowRequestAsync))]
    public async Task<IActionResult> GetAllCardHolderBorrowRequestAsync([FromRoute] Guid userId,
        [FromQuery] int? pageIndex, [FromQuery] int? pageSize)
    {
        return Ok(await _borrowReqSvc.GetAllCardHolderBorrowRequestByUserIdAsync(
            userId: userId,
            pageIndex: pageIndex ?? 1,
            pageSize: pageSize ?? _appSettings.PageSize));
    }

    [Authorize]
    [HttpGet(APIRoute.LibraryCardHolder.GetAllCardHolderBorrowRecord, Name = nameof(GetAllCardHolderBorrowRecordAsync))]
    public async Task<IActionResult> GetAllCardHolderBorrowRecordAsync([FromRoute] Guid userId,
        [FromQuery] int? pageIndex, [FromQuery] int? pageSize)
    {
        return Ok(await _borrowRecSvc.GetAllCardHolderBorrowRecordByUserIdAsync(
            userId: userId,
            pageIndex: pageIndex ?? 1,
            pageSize: pageSize ?? _appSettings.PageSize));
    }

    [Authorize]
    [HttpGet(APIRoute.LibraryCardHolder.GetAllCardHolderDigitalBorrow,
        Name = nameof(GetAllCardHolderDigitalBorrowAsync))]
    public async Task<IActionResult> GetAllCardHolderDigitalBorrowAsync([FromRoute] Guid userId,
        [FromQuery] int? pageIndex, [FromQuery] int? pageSize)
    {
        return Ok(await _digitalBorrowSvc.GetAllCardHolderDigitalBorrowByUserIdAsync(
            userId: userId,
            pageIndex: pageIndex ?? 1,
            pageSize: pageSize ?? _appSettings.PageSize));
    }

    [Authorize]
    [HttpGet(APIRoute.LibraryCardHolder.GetAllCardHolderReservation, Name = nameof(GetAllCardHolderReservationAsync))]
    public async Task<IActionResult> GetAllCardHolderReservationAsync([FromRoute] Guid userId,
        [FromQuery] int? pageIndex, [FromQuery] int? pageSize)
    {
        return Ok(await _reservationQueueSvc.GetAllCardHolderReservationByUserIdAsync(
            userId: userId,
            pageIndex: pageIndex ?? 1,
            pageSize: pageSize ?? _appSettings.PageSize));
    }

    [Authorize]
    [HttpGet(APIRoute.LibraryCardHolder.GetAllCardHolderInvoice, Name = nameof(GetAllCardHolderInvoiceAsync))]
    public async Task<IActionResult> GetAllCardHolderInvoiceAsync([FromRoute] Guid userId,
        [FromQuery] int? pageIndex, [FromQuery] int? pageSize)
    {
        return Ok(await _invoiceSvc.GetAllCardHolderInvoiceByUserIdAsync(
            userId: userId,
            pageIndex: pageIndex ?? 1,
            pageSize: pageSize ?? _appSettings.PageSize));
    }

    [Authorize]
    [HttpGet(APIRoute.LibraryCardHolder.GetAllCardHolderTransaction, Name = nameof(GetAllCardHolderTransactionAsync))]
    public async Task<IActionResult> GetAllCardHolderTransactionAsync([FromRoute] Guid userId,
        [FromQuery] int? pageIndex, [FromQuery] int? pageSize)
    {
        return Ok(await _transactionSvc.GetAllCardHolderTransactionByUserIdAsync(
            userId: userId,
            pageIndex: pageIndex ?? 1,
            pageSize: pageSize ?? _appSettings.PageSize));
    }

    [Authorize]
    [HttpGet(APIRoute.LibraryCardHolder.GetAllCardHolderNotification, Name = nameof(GetAllCardHolderNotificationAsync))]
    public async Task<IActionResult> GetAllCardHolderNotificationAsync([FromRoute] Guid userId,
        [FromQuery] int? pageIndex, [FromQuery] int? pageSize)
    {
        return Ok(await _notificationSvc.GetAllCardHolderNotificationByUserIdAsync(
            userId: userId,
            pageIndex: pageIndex ?? 1,
            pageSize: pageSize ?? _appSettings.PageSize));
    }

    [Authorize]
    [HttpPatch(APIRoute.LibraryCardHolder.SoftDeleteCardHolder, Name = nameof(SoftDeleteCardHolderAsync))]
    public async Task<IActionResult> SoftDeleteCardHolderAsync([FromRoute] Guid userId)
    {
        return Ok(await _userSvc.SoftDeleteLibraryCardHolderAsync(userId: userId));
    }

    [Authorize]
    [HttpPatch(APIRoute.LibraryCardHolder.SoftDeleteRangeCardHolder, Name = nameof(SoftDeleteRangeCardHolderAsync))]
    public async Task<IActionResult> SoftDeleteRangeCardHolderAsync([FromBody] RangeRequest<Guid> req)
    {
        return Ok(await _userSvc.SoftDeleteRangeLibraryCardHolderAsync(userIds: req.Ids));
    }

    [Authorize]
    [HttpPatch(APIRoute.LibraryCardHolder.UndoDeleteCardHolder, Name = nameof(UndoDeleteCardHolderAsync))]
    public async Task<IActionResult> UndoDeleteCardHolderAsync([FromRoute] Guid userId)
    {
        return Ok(await _userSvc.UndoDeleteLibraryCardHolderAsync(userId: userId));
    }

    [Authorize]
    [HttpPatch(APIRoute.LibraryCardHolder.UndoDeleteRangeCardHolder, Name = nameof(UndoDeleteRangeCardHolderAsync))]
    public async Task<IActionResult> UndoDeleteRangeCardHolderAsync([FromBody] RangeRequest<Guid> req)
    {
        return Ok(await _userSvc.UndoDeleteRangeLibraryCardHolderAsync(userIds: req.Ids));
    }

    [Authorize]
    [HttpDelete(APIRoute.LibraryCardHolder.DeleteCardHolder, Name = nameof(DeleteCardHolderAsync))]
    public async Task<IActionResult> DeleteCardHolderAsync([FromRoute] Guid userId)
    {
        return Ok(await _userSvc.DeleteLibraryCardHolderAsync(userId: userId));
    }

    [Authorize]
    [HttpDelete(APIRoute.LibraryCardHolder.DeleteRangeCardHolder, Name = nameof(DeleteRangeCardHolderAsync))]
    public async Task<IActionResult> DeleteRangeCardHolderAsync([FromBody] RangeRequest<Guid> req)
    {
        return Ok(await _userSvc.DeleteRangeLibraryCardHolderAsync(userIds: req.Ids));
    }

    #endregion

    [Authorize]
    [HttpGet(APIRoute.LibraryCardHolder.GetByBarcode, Name = nameof(GetLibraryCardHolderByBarcodeAsync))]
    public async Task<IActionResult> GetLibraryCardHolderByBarcodeAsync([FromQuery] string barcode)
    {
        return Ok(await _userSvc.GetLibraryCardHolderByBarcodeAsync(barcode: barcode));
    }
}