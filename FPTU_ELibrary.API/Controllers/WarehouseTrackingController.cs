using FPTU_ELibrary.API.Extensions;
using FPTU_ELibrary.API.Payloads;
using FPTU_ELibrary.API.Payloads.Requests.WarehouseTracking;
using FPTU_ELibrary.Application.Configurations;
using FPTU_ELibrary.Application.Dtos.LibraryItems;
using FPTU_ELibrary.Domain.Common.Enums;
using FPTU_ELibrary.Domain.Interfaces.Services;
using FPTU_ELibrary.Domain.Specifications;
using FPTU_ELibrary.Domain.Specifications.Params;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace FPTU_ELibrary.API.Controllers;

[ApiController]
public class WarehouseTrackingController : ControllerBase
{
    private readonly IWarehouseTrackingService<WarehouseTrackingDto> _warehouseTrackSvc;
    private readonly AppSettings _appSettings;

    public WarehouseTrackingController(
        IWarehouseTrackingService<WarehouseTrackingDto> warehouseTrackSvc,
        IOptionsMonitor<AppSettings> monitor)
    {
        _warehouseTrackSvc = warehouseTrackSvc;
        _appSettings = monitor.CurrentValue;
    }
    
    [Authorize]
    [HttpPost(APIRoute.WarehouseTracking.Create, Name = nameof(CreateWarehouseTrackingAsync))]
    public async Task<IActionResult> CreateWarehouseTrackingAsync([FromForm] CreateWarehouseTrackingRequest req)
    {
        return Ok(await _warehouseTrackSvc.CreateAndImportDetailsAsync(
            dto: req.ToWarehouseTrackingDto(),
            trackingDetailsFile: req.File,
            scanningFields: req.ScanningFields,
            duplicateHandle: req.DuplicateHandle
        ));
    }

    [Authorize]
    [HttpGet(APIRoute.WarehouseTracking.GetAll, Name = nameof(GetAllWarehouseTrackingAsync))]
    public async Task<IActionResult> GetAllWarehouseTrackingAsync([FromQuery] WarehouseTrackingSpecParams specParams)
    {
        return Ok(await _warehouseTrackSvc.GetAllWithSpecAsync(new WarehouseTrackingSpecification(
            specParams: specParams,
            pageIndex: specParams.PageIndex ?? 1,
            pageSize: specParams.PageSize ?? _appSettings.PageSize)));
    }

    [Authorize]
    [HttpGet(APIRoute.WarehouseTracking.GetById, Name = nameof(GetWarehouseByIdAsync))]
    public async Task<IActionResult> GetWarehouseByIdAsync([FromRoute] int id)
    {
        return Ok(await _warehouseTrackSvc.GetByIdAsync(id));
    }
    
    [Authorize(Roles = nameof(Role.HeadLibrarian))]
    [HttpPatch(APIRoute.WarehouseTracking.UpdateStatus, Name = nameof(UpdateWarehouseTrackingStatusAsync))]
    public async Task<IActionResult> UpdateWarehouseTrackingStatusAsync(
        [FromRoute] int id, 
        [FromQuery] WarehouseTrackingStatus status)
    {
        return Ok(await _warehouseTrackSvc.UpdateStatusAsync(id, status));
    }

    [Authorize]
    [HttpPut(APIRoute.WarehouseTracking.Update, Name = nameof(UpdateWarehouseTrackingAsync))]
    public async Task<IActionResult> UpdateWarehouseTrackingAsync(
        [FromRoute] int id, 
        [FromBody] UpdateWarehouseTrackingRequest req)
    {
        return Ok(await _warehouseTrackSvc.UpdateAsync(id, req.ToWarehouseTrackingDto()));
    }
    
    [Authorize]
    [HttpDelete(APIRoute.WarehouseTracking.Delete, Name = nameof(DeleteWarehouseByIdAsync))]
    public async Task<IActionResult> DeleteWarehouseByIdAsync([FromRoute] int id)
    {
        return Ok(await _warehouseTrackSvc.DeleteAsync(id));
    }
}