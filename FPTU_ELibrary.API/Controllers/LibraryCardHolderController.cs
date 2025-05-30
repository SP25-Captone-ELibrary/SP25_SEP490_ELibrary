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
    private readonly ILibraryCardService<LibraryCardDto> _libCardSvc;
    private readonly IBorrowRequestService<BorrowRequestDto> _borrowReqSvc;
    private readonly IBorrowRecordService<BorrowRecordDto> _borrowRecSvc;
    private readonly ITransactionService<TransactionDto> _transactionSvc;
    private readonly INotificationService<NotificationDto> _notificationSvc;
    private readonly IDigitalBorrowService<DigitalBorrowDto> _digitalBorrowSvc;
    private readonly IReservationQueueService<ReservationQueueDto> _reservationQueueSvc;

    public LibraryCardHolderController(
        IUserService<UserDto> userSvc,
        ILibraryCardService<LibraryCardDto> libCardSvc,
        IDigitalBorrowService<DigitalBorrowDto> digitalBorrowSvc,
        IBorrowRequestService<BorrowRequestDto> borrowReqSvc,
        IBorrowRecordService<BorrowRecordDto> borrowRecSvc,
        IReservationQueueService<ReservationQueueDto> reservationQueueSvc,
        ITransactionService<TransactionDto> transactionSvc,
        INotificationService<NotificationDto> notificationSvc,
        IOptionsMonitor<AppSettings> monitor1)
    {
        _userSvc = userSvc;
        _libCardSvc = libCardSvc;
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
        var processedByEmail = HttpContext.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
        return Ok(await _userSvc.CreateLibraryCardHolderAsync(
            createdByEmail: processedByEmail ?? string.Empty,
            dto: req.ToLibraryCardHolderDto(),
            transactionMethod: req.TransactionMethod,
            paymentMethodId: req.PaymentMethodId,
            libraryCardPackageId: req.LibraryCardPackageId));
    }

    [Authorize]
    [HttpPost(APIRoute.LibraryCardHolder.AddCard, Name = nameof(AddLibraryCardAsync))]
    public async Task<IActionResult> AddLibraryCardAsync([FromBody] AddLibraryCardRequest req)
    {
        var processedByEmail = HttpContext.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
        return Ok(await _libCardSvc.RegisterCardByEmployeeAsync(
            processedByEmail: processedByEmail ?? string.Empty,
            userId: req.UserId,
            method: req.TransactionMethod,
            dto: req.ToUserWithLibraryCardDto(),
            paymentMethodId: req.PaymentMethodId,
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
    [HttpGet(APIRoute.LibraryCardHolder.GetCardHolderBorrowRequestById, Name = nameof(GetCardHolderBorrowRequestByIdAsync))]
    public async Task<IActionResult> GetCardHolderBorrowRequestByIdAsync([FromRoute] Guid userId, [FromRoute] int requestId)
    {
        return Ok(await _borrowReqSvc.GetByIdAsync(id: requestId, userId: userId));
    }

    [Authorize]
    [HttpGet(APIRoute.LibraryCardHolder.GetCardHolderBorrowRecordById, Name = nameof(GetCardHolderBorrowRecordByIdAsync))]
    public async Task<IActionResult> GetCardHolderBorrowRecordByIdAsync([FromRoute] Guid userId, [FromRoute] int borrowRecordId)
    {
        return Ok(await _borrowRecSvc.GetByIdAsync(id: borrowRecordId, userId: userId));
    }

    [Authorize]
    [HttpGet(APIRoute.LibraryCardHolder.GetCardHolderDigitalBorrowById, Name = nameof(GetCardHolderDigitalBorrowByIdAsync))]
    public async Task<IActionResult> GetCardHolderDigitalBorrowByIdAsync([FromRoute] Guid userId, [FromRoute] int digitalBorrowId)
    {
        return Ok(await _digitalBorrowSvc.GetByIdAsync(id: digitalBorrowId, userId: userId));
    }

    [Authorize]
    [HttpGet(APIRoute.LibraryCardHolder.GetCardHolderTransactionById, Name = nameof(GetCardHolderTransactionByIdAsync))]
    public async Task<IActionResult> GetCardHolderTransactionByIdAsync([FromRoute] Guid userId, [FromRoute] int transactionId)
    {
        return Ok(await _transactionSvc.GetByIdAsync(id: transactionId, userId: userId));
    }

    [Authorize]
    [HttpGet(APIRoute.LibraryCardHolder.GetCardHolderReservationById, Name = nameof(GetCardHolderReservationByIdAsync))]
    public async Task<IActionResult> GetCardHolderReservationByIdAsync([FromRoute] Guid userId, [FromRoute] int reservationId)
    {
        return Ok(await _reservationQueueSvc.GetByIdAsync(id: reservationId, userId: userId));
    }
    
    [Authorize]
    [HttpGet(APIRoute.LibraryCardHolder.GetAllCardHolderBorrowRequest, Name = nameof(GetAllCardHolderBorrowRequestAsync))]
    public async Task<IActionResult> GetAllCardHolderBorrowRequestAsync([FromRoute] Guid userId,
        [FromQuery] BorrowRequestSpecParams specParams)
    {
        return Ok(await _borrowReqSvc.GetAllWithSpecAsync(
            new BorrowRequestSpecification(
                specParams: specParams,
                pageIndex: specParams.PageIndex ?? 1,
                pageSize: specParams.PageSize ?? _appSettings.PageSize,
                userId: userId)));
    }

    [Authorize]
    [HttpGet(APIRoute.LibraryCardHolder.GetAllCardHolderBorrowRecord, Name = nameof(GetAllCardHolderBorrowRecordAsync))]
    public async Task<IActionResult> GetAllCardHolderBorrowRecordAsync([FromRoute] Guid userId, [FromQuery] BorrowRecordSpecParams specParams)
    {
        return Ok(await _borrowRecSvc.GetAllWithSpecAsync(
            new BorrowRecordSpecification(
                specParams: specParams,
                pageIndex: specParams.PageIndex ?? 1,
                pageSize: specParams.PageSize ?? _appSettings.PageSize,
                userId: userId)));
    }

    [Authorize]
    [HttpGet(APIRoute.LibraryCardHolder.GetAllCardHolderDigitalBorrow, Name = nameof(GetAllCardHolderDigitalBorrowAsync))]
    public async Task<IActionResult> GetAllCardHolderDigitalBorrowAsync([FromRoute] Guid userId, [FromQuery] DigitalBorrowSpecParams specParams)
    {
        return Ok(await _digitalBorrowSvc.GetAllWithSpecAsync(
            new DigitalBorrowSpecification(
                specParams: specParams,
                pageIndex: specParams.PageIndex ?? 1,
                pageSize: specParams.PageSize ?? _appSettings.PageSize,
                userId: userId)));
    }

    [Authorize]
    [HttpGet(APIRoute.LibraryCardHolder.GetAllCardHolderReservation, Name = nameof(GetAllCardHolderReservationAsync))]
    public async Task<IActionResult> GetAllCardHolderReservationAsync([FromRoute] Guid userId, [FromQuery] ReservationQueueSpecParams specParams)
    {
        return Ok(await _reservationQueueSvc.GetAllCardHolderReservationAsync(new ReservationQueueSpecification(
            specParams: specParams,
            pageIndex: specParams.PageIndex ?? 1,
            pageSize: specParams.PageSize ?? _appSettings.PageSize,
            userId: userId)));
    }

    [Authorize]
    [HttpGet(APIRoute.LibraryCardHolder.GetAllCardHolderTransaction, Name = nameof(GetAllCardHolderTransactionAsync))]
    public async Task<IActionResult> GetAllCardHolderTransactionAsync([FromRoute] Guid userId, [FromQuery] TransactionSpecParams specParams)
    {
        return Ok(await _transactionSvc.GetAllCardHolderTransactionAsync(
            new TransactionSpecification(
                specParams: specParams,
                pageIndex: specParams.PageIndex ?? 1,
                pageSize: specParams.PageSize ?? _appSettings.PageSize,
                userId: userId)));
    }

    [Authorize]
    [HttpGet(APIRoute.LibraryCardHolder.GetAllCardHolderNotification, Name = nameof(GetAllCardHolderNotificationAsync))]
    public async Task<IActionResult> GetAllCardHolderNotificationAsync([FromRoute] Guid userId, [FromQuery] NotificationSpecParams specParams)
    {
        return Ok(await _notificationSvc.GetAllWithSpecAsync(new NotificationSpecification(
            specParams: specParams,
            pageIndex: specParams.PageIndex ?? 1,
            pageSize: specParams.PageSize ?? _appSettings.PageSize,
            isCallFromManagement: true)));
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