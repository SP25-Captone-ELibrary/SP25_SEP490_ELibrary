using System.Security.Claims;
using FPTU_ELibrary.API.Extensions;
using FPTU_ELibrary.API.Payloads;
using FPTU_ELibrary.API.Payloads.Requests;
using FPTU_ELibrary.API.Payloads.Requests.Auth;
using FPTU_ELibrary.API.Payloads.Requests.User;
using FPTU_ELibrary.Application.Configurations;
using FPTU_ELibrary.Application.Dtos;
using FPTU_ELibrary.Application.Dtos.Borrows;
using FPTU_ELibrary.Application.Dtos.Payments;
using FPTU_ELibrary.Domain.Common.Enums;
using FPTU_ELibrary.Domain.Interfaces.Services;
using FPTU_ELibrary.Domain.Specifications;
using FPTU_ELibrary.Domain.Specifications.Params;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Nest;

namespace FPTU_ELibrary.API.Controllers;

[ApiController]
public class UserController : ControllerBase
{
    private readonly IUserService<UserDto> _userService;
    private readonly IBorrowRequestService<BorrowRequestDto> _borrowReqSvc;
    private readonly IBorrowRecordService<BorrowRecordDto> _borrowRecSvc;
    private readonly ITransactionService<TransactionDto> _transactionSvc;
    private readonly IDigitalBorrowService<DigitalBorrowDto> _digitalBorrowSvc;
    private readonly IReservationQueueService<ReservationQueueDto> _reservationQueueSvc;
    
    private readonly AppSettings _appSettings;

    public UserController(
        IBorrowRequestService<BorrowRequestDto> borrowReqSvc,
        IBorrowRecordService<BorrowRecordDto> borrowRecSvc,
        IDigitalBorrowService<DigitalBorrowDto> digitalBorrowSvc,
        ITransactionService<TransactionDto> transactionSvc,
        IReservationQueueService<ReservationQueueDto> reservationQueueSvc,
        IOptionsMonitor<AppSettings> monitor,
        IUserService<UserDto> userService)
    {
        _userService = userService;
        _borrowReqSvc = borrowReqSvc;
        _borrowRecSvc = borrowRecSvc;
        _transactionSvc = transactionSvc;
        _digitalBorrowSvc = digitalBorrowSvc;
        _reservationQueueSvc = reservationQueueSvc;
        _appSettings = monitor.CurrentValue;
    }

    #region Management
    [Authorize]
    [HttpGet(APIRoute.User.GetAll, Name = nameof(GetAllUserAsync))]
    public async Task<IActionResult> GetAllUserAsync([FromQuery] UserSpecParams req)
    {
        return Ok(await _userService.GetAllWithSpecAsync(new UserSpecification(
             userSpecParams: req,
             pageIndex: req.PageIndex ?? 1,
             pageSize: req.PageSize ?? _appSettings.PageSize), tracked: false));
    }
     
    [Authorize]
    [HttpGet(APIRoute.User.GetById, Name = nameof(GetUserByIdAsync))]
    public async Task<IActionResult> GetUserByIdAsync([FromRoute] Guid id)
    {
        return Ok(await _userService.GetByIdAsync(id));
    }

    [Authorize]
    [HttpPost(APIRoute.User.Create, Name = nameof(CreateUserAsync))]
    public async Task<IActionResult> CreateUserAsync([FromBody] CreateUserRequest req)
    {   
        return Ok(await _userService.CreateAccountByAdminAsync(req.ToUser()));
    }
    
    [Authorize]
    [HttpPatch(APIRoute.User.ChangeAccountStatus,Name=nameof(ChangeAccountStatus))]
    public async Task<IActionResult> ChangeAccountStatus([FromRoute] Guid id)
    {  
        return Ok(await _userService.ChangeActiveStatusAsync(id));
    }

    [Authorize]
    [HttpPut(APIRoute.User.Update, Name = nameof(UpdateUserAsync))]
    public async Task<IActionResult> UpdateUserAsync([FromRoute] Guid id, [FromBody] UpdateUserRequest dto)
    {
        return Ok(await _userService.UpdateAsync(id, dto.ToUserForUpdate()));
    }

    [Authorize]
    [HttpDelete(APIRoute.User.SoftDelete, Name = nameof(SoftDeleteUserAsync))]
    public async Task<IActionResult> SoftDeleteUserAsync([FromRoute] Guid id)
    {
        return Ok(await _userService.SoftDeleteAsync(id));
    }
    
    [Authorize]
    [HttpDelete(APIRoute.User.SoftDeleteRange, Name = nameof(SoftDeleteRangeUserAsync))]
    public async Task<IActionResult> SoftDeleteRangeUserAsync([FromBody] RangeRequest<Guid> req)
    {
        return Ok(await _userService.SoftDeleteRangeAsync(req.Ids));
    }
    
    [Authorize]
    [HttpDelete(APIRoute.User.UndoDelete, Name = nameof(UndoDeleteUserAsync))]
    public async Task<IActionResult> UndoDeleteUserAsync([FromRoute] Guid id)
    {
        return Ok(await _userService.UndoDeleteAsync(id));
    }
    
    [Authorize]
    [HttpDelete(APIRoute.User.UndoDeleteRange, Name = nameof(UndoDeleteRangeUserAsync))]
    public async Task<IActionResult> UndoDeleteRangeUserAsync([FromBody] RangeRequest<Guid> req)
    {
        return Ok(await _userService.UndoDeleteRangeAsync(req.Ids));
    }
        
    [Authorize]
    [HttpDelete(APIRoute.User.HardDelete,Name = nameof(DeleteUserAsync))]
    public async Task<IActionResult> DeleteUserAsync([FromRoute] Guid id)
    {
        return Ok(await _userService.DeleteAsync(id));
    }
    
    [Authorize]
    [HttpDelete(APIRoute.User.HardDeleteRange, Name = nameof(DeleteRangeUserAsync))]
    public async Task<IActionResult> DeleteRangeUserAsync([FromBody] RangeRequest<Guid> req)
    {
        return Ok(await _userService.DeleteRangeAsync(req.Ids));
    }
    
    [Authorize]
    [HttpPost(APIRoute.User.Import, Name = nameof(ImportUserAsync))]
    public async Task<IActionResult> ImportUserAsync([FromForm] CreateManyUsersRequest req)
    { 
        var email = User.FindFirst(ClaimTypes.Email)?.Value ?? "";
        return Ok(await _userService.CreateManyAccountsWithSendEmail(
            email: email, excelFile: req.File, 
            duplicateHandle: req.DuplicateHandle, isSendEmail: req.IsSendEmail));
    }
    
    [Authorize]
    [HttpGet(APIRoute.User.Export, Name = nameof(ExportUserAsync))]
    public async Task<IActionResult> ExportUserAsync([FromQuery] UserSpecParams specParams)
    {
        var exportResult = await _userService.ExportAsync(new UserSpecification(
            userSpecParams: specParams,
            pageIndex: specParams.PageIndex ?? 1,
            pageSize: specParams.PageSize ?? _appSettings.PageSize));

        return exportResult.Data is byte[] fileStream
            ? File(fileStream, @"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Employees.xlsx")
            : Ok(exportResult);
    }
    #endregion

    [Authorize]
    [HttpGet(APIRoute.User.GetAllPendingActivity, Name = nameof(GetAllPendingActivityAsync))]
    public async Task<IActionResult> GetAllPendingActivityAsync()
    {
        var email = HttpContext.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
        return Ok(await _userService.GetPendingLibraryActivitySummaryByEmailAsync(email: email ?? string.Empty));
    }
    
    [Authorize]
    [HttpGet(APIRoute.User.GetAllUserBorrowRequest, Name = nameof(GetAllUserBorrowRequestAsync))]
    public async Task<IActionResult> GetAllUserBorrowRequestAsync([FromQuery] BorrowRequestSpecParams specParams)
    {
        var email = HttpContext.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
        return Ok(await _borrowReqSvc.GetAllWithSpecAsync(
            specification: new BorrowRequestSpecification(
                specParams: specParams,
                pageIndex: specParams.PageIndex ?? 1,
                pageSize: specParams.PageSize ?? _appSettings.PageSize,
                email: email ?? string.Empty)));
    }
    
    [Authorize]
    [HttpGet(APIRoute.User.GetBorrowRequestById, Name = nameof(GetUserBorrowRequestByIdAsync))]
    public async Task<IActionResult> GetUserBorrowRequestByIdAsync([FromRoute] int id)
    {
        var email = HttpContext.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
        return Ok(await _borrowReqSvc.GetByIdAsync(id: id, email: email ?? string.Empty));
    }
    
    [Authorize]
    [HttpGet(APIRoute.User.GetAllUserBorrowRecord, Name = nameof(GetAllUserBorrowRecordAsync))]
    public async Task<IActionResult> GetAllUserBorrowRecordAsync([FromQuery] BorrowRecordSpecParams specParams)
    {
        var email = HttpContext.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
        return Ok(await _borrowRecSvc.GetAllWithSpecAsync(
            specification: new BorrowRecordSpecification(
                specParams: specParams,
                pageIndex: specParams.PageIndex ?? 1,
                pageSize: specParams.PageSize ?? _appSettings.PageSize,
                email: email ?? string.Empty)));
    }
    
    [Authorize]
    [HttpGet(APIRoute.User.GetBorrowRecordById, Name = nameof(GetUserBorrowRecordByIdAsync))]
    public async Task<IActionResult> GetUserBorrowRecordByIdAsync([FromRoute] int id)
    {
        var email = HttpContext.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
        return Ok(await _borrowRecSvc.GetByIdAsync(id: id, email: email ?? string.Empty));
    }
    
    [Authorize]
    [HttpGet(APIRoute.User.GetAllUserDigitalBorrow, Name = nameof(GetAllUserDigitalBorrowAsync))]
    public async Task<IActionResult> GetAllUserDigitalBorrowAsync([FromQuery] DigitalBorrowSpecParams specParams)
    {
        var email = HttpContext.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
        return Ok(await _digitalBorrowSvc.GetAllWithSpecAsync(
            specification: new DigitalBorrowSpecification(
                specParams: specParams,
                pageIndex: specParams.PageIndex ?? 1,
                pageSize: specParams.PageSize ?? _appSettings.PageSize,
                email: email ?? string.Empty)));
    }
    
    [Authorize]
    [HttpGet(APIRoute.User.GetDigitalBorrowById, Name = nameof(GetUserDigitalBorrowByIdAsync))]
    public async Task<IActionResult> GetUserDigitalBorrowByIdAsync([FromRoute] int id)
    {
        var email = HttpContext.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
        return Ok(await _digitalBorrowSvc.GetByIdAsync(id: id, email: email ?? string.Empty));
    }

    [Authorize]
    [HttpGet(APIRoute.User.GetAllUserTransaction, Name = nameof(GetAllUserTransactionAsync))]
    public async Task<IActionResult> GetAllUserTransactionAsync([FromQuery] TransactionSpecParams specParams)
    {
        var email = HttpContext.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
        return Ok(await _transactionSvc.GetAllCardHolderTransactionAsync(
            spec: new TransactionSpecification(
                specParams: specParams,
                pageIndex: specParams.PageIndex ?? 1,
                pageSize: specParams.PageSize ?? _appSettings.PageSize,
                email: email ?? string.Empty)));
    }
    
    [Authorize]
    [HttpGet(APIRoute.User.GetTransactionById, Name = nameof(GetUserTransactionByIdAsync))]
    public async Task<IActionResult> GetUserTransactionByIdAsync([FromRoute] int id)
    {
        var email = HttpContext.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
        return Ok(await _transactionSvc.GetByIdAsync(id: id, email: email));
    }
    
    [Authorize]
    [HttpGet(APIRoute.User.GetAllUserReservation, Name = nameof(GetAllUserReservationAsync))]
    public async Task<IActionResult> GetAllUserReservationAsync([FromQuery] ReservationQueueSpecParams specParams)
    {
        var email = HttpContext.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
        return Ok(await _reservationQueueSvc.GetAllCardHolderReservationAsync(
            spec: new ReservationQueueSpecification(
                specParams: specParams,
                pageIndex: specParams.PageIndex ?? 1,
                pageSize: specParams.PageSize ?? _appSettings.PageSize,
                email: email ?? string.Empty)));
    }
    
    [Authorize]
    [HttpGet(APIRoute.User.GetReservationById, Name = nameof(GetUserReservationByIdAsync))]
    public async Task<IActionResult> GetUserReservationByIdAsync([FromRoute] int id)
    {
        var email = HttpContext.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
        return Ok(await _reservationQueueSvc.GetByIdAsync(id: id, email: email));
    }
    
    [Authorize]
    [HttpGet(APIRoute.User.CalculateBorrowReturnSummary, Name = nameof(CalculateBorrowReturnSummaryAsync))]
    public async Task<IActionResult> CalculateBorrowReturnSummaryAsync()
    {
        var email = HttpContext.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
        return Ok(await _borrowRecSvc.CalculateBorrowReturnSummaryAsync(email: email ?? string.Empty));
    }
}