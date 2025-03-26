using FPTU_ELibrary.API.Payloads;
using FPTU_ELibrary.API.Payloads.Requests;
using FPTU_ELibrary.API.Payloads.Requests.Reservation;
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
public class ReservationController : ControllerBase
{
    private readonly IReservationQueueService<ReservationQueueDto> _reservationQueueSvc;
    private readonly AppSettings _appSettings;

    public ReservationController(
        IReservationQueueService<ReservationQueueDto> reservationQueueSvc,
        IOptionsMonitor<AppSettings> monitor)
    {
        _appSettings = monitor.CurrentValue;
        _reservationQueueSvc = reservationQueueSvc;
    }

    [Authorize]
    [HttpGet(APIRoute.Reservation.GetAll, Name = nameof(GetAllReservationAsync))]
    public async Task<IActionResult> GetAllReservationAsync([FromQuery] ReservationQueueSpecParams specParams)
    {
        return Ok(await _reservationQueueSvc.GetAllCardHolderReservationAsync(new ReservationQueueSpecification(
            specParams: specParams,
            pageIndex: specParams.PageIndex ?? 1,
            pageSize: specParams.PageSize ?? _appSettings.PageSize)));
    }

    [Authorize]
    [HttpGet(APIRoute.Reservation.GetById, Name = nameof(GetReservationByIdAsync))]
    public async Task<IActionResult> GetReservationByIdAsync([FromRoute] int id)
    {
        return Ok(await _reservationQueueSvc.GetByIdAsync(id, email: null, userId: null));
    }

    [Authorize]
    [HttpGet(APIRoute.Reservation.GetAssignableById, Name = nameof(GetAssignableByIdAsync))]
    public async Task<IActionResult> GetAssignableByIdAsync([FromRoute] int id)
    {
        return Ok(await _reservationQueueSvc.GetAssignableByIdAsync(id: id));
    }
    
    [Authorize]
    [HttpGet(APIRoute.Reservation.GetAllAssignableAfterReturn, Name = nameof(GetAllAssignableAfterReturnAsync))]
    public async Task<IActionResult> GetAllAssignableAfterReturnAsync([FromQuery] GetAllAssignableAfterReturnRequest req)
    {
        return Ok(await _reservationQueueSvc.GetAssignableInstancesAfterReturnAsync(libraryItemInstanceIds: req.LibraryItemInstanceIds));
    }

    [Authorize]
    [HttpPost(APIRoute.Reservation.AssignAfterReturn, Name = nameof(AssignItemInstancesAsync))]
    public async Task<IActionResult> AssignItemInstancesAsync([FromBody] AssignInstancesAfterReturnRequest req)
    {
        return Ok(await _reservationQueueSvc.AssignInstancesAfterReturnAsync(libraryItemInstanceIds: req.LibraryItemInstanceIds));
    }
    
    [Authorize]
    [HttpPost(APIRoute.Reservation.AssignById, Name = nameof(AssignItemInstanceByIdAsync))]
    public async Task<IActionResult> AssignItemInstanceByIdAsync([FromRoute] int id, [FromQuery] int libraryItemInstanceId)
    {
        return Ok(await _reservationQueueSvc.AssignByIdAndInstanceIdAsync(id: id, libraryItemInstanceId: libraryItemInstanceId));
    }
    
    [Authorize]
    [HttpPatch(APIRoute.Reservation.ConfirmApplyLabel, Name = nameof(ConfirmApplyReservationLabelAsync))]
    public async Task<IActionResult> ConfirmApplyReservationLabelAsync([FromBody] ConfirmApplyReservationLabelRequest req)
    {
        return Ok(await _reservationQueueSvc.ConfirmApplyLabelAsync(queueIds: req.QueueIds));
    }
}