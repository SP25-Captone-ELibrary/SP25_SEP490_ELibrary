using FPTU_ELibrary.API.Extensions;
using FPTU_ELibrary.API.Payloads;
using FPTU_ELibrary.API.Payloads.Requests.WarehouseTrackingDetail;
using FPTU_ELibrary.Application.Dtos.LibraryItems;
using FPTU_ELibrary.Domain.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FPTU_ELibrary.API.Controllers;

[ApiController]
public class WarehouseTrackingDetailController : ControllerBase
{
    private readonly IWarehouseTrackingDetailService<WarehouseTrackingDetailDto> _trackingDetailService;

    public WarehouseTrackingDetailController(IWarehouseTrackingDetailService<WarehouseTrackingDetailDto>
        trackingDetailService)
    {
        _trackingDetailService = trackingDetailService;
    }
    
    [Authorize]
    [HttpPost(APIRoute.WarehouseTrackingDetail.Import, Name = nameof(ImportWarehouseTrackingDetailAsync))]
    public async Task<IActionResult> ImportWarehouseTrackingDetailAsync(
        [FromRoute] int trackingId,
        [FromForm] ImportTrackingDetailRequest req)
    {
        return Ok(await _trackingDetailService.ImportAsync(
            trackingId: trackingId,
            file: req.File,
            scanningFields: req.ScanningFields,
            duplicateHandle: req.DuplicateHandle));
    }

    [Authorize]
    [HttpPut(APIRoute.WarehouseTrackingDetail.Update, Name = nameof(UpdateWarehouseTrackingDetailAsync))]
    public async Task<IActionResult> UpdateWarehouseTrackingDetailAsync(
        [FromRoute] int id, [FromBody] UpdateWarehouseTrackingDetailRequest req)
    {
        return Ok(await _trackingDetailService.UpdateAsync(id, req.ToWarehouseTrackingDetailDto()));
    }
    
    [Authorize]
    [HttpPut(APIRoute.WarehouseTrackingDetail.UpdateItem, Name = nameof(UpdateItemForWarehouseTrackingDetailAsync))]
    public async Task<IActionResult> UpdateItemForWarehouseTrackingDetailAsync(
        [FromRoute] int id, [FromQuery] int libraryItemId)
    {
        return Ok(await _trackingDetailService.UpdateItemFromExternalAsync(
            trackingDetailId: id, libraryItemId: libraryItemId));
    }
    
    [Authorize]
    [HttpPost(APIRoute.WarehouseTrackingDetail.AddToTracking, Name = nameof(AddToWarehouseTrackingAsync))]
    public async Task<IActionResult> AddToWarehouseTrackingAsync([FromRoute] int trackingId, [FromBody] CreateWarehouseTrackingDetailRequest req)
    {
        return Ok(await _trackingDetailService.AddToWarehouseTrackingAsync(
            trackingId: trackingId, dto: req.ToWarehouseTrackingDetailDto()));
    }
    
    [Authorize]
    [HttpDelete(APIRoute.WarehouseTrackingDetail.DeleteItem, Name = nameof(RemoveItemForWarehouseTrackingAsync))]
    public async Task<IActionResult> RemoveItemForWarehouseTrackingAsync([FromRoute] int id, [FromQuery] int libraryItemId)
    {
        return Ok(await _trackingDetailService.DeleteItemAsync(trackingDetailId: id, libraryItemId: libraryItemId));
    }
    
    [Authorize]
    [HttpDelete(APIRoute.WarehouseTrackingDetail.Delete, Name = nameof(DeleteWarehouseTrackingAsync))]
    public async Task<IActionResult> DeleteWarehouseTrackingAsync([FromRoute] int id)
    {
        return Ok(await _trackingDetailService.DeleteAsync(id));
    }

    [Authorize]
    [HttpGet(APIRoute.WarehouseTrackingDetail.GetById, Name = nameof(GetWarehouseTrackingDetailByIdAsync))]
    public async Task<IActionResult> GetWarehouseTrackingDetailByIdAsync([FromRoute] int id)
    {
        return Ok(await _trackingDetailService.GetByIdAsync(id));
    }
    
    [Authorize]
    [HttpGet(APIRoute.WarehouseTrackingDetail.GetAllByTrackingId, Name = nameof(GetAllDetailByTrackingIdAsync))]
    public async Task<IActionResult> GetAllDetailByTrackingIdAsync([FromRoute] int trackingId)
    {
        return Ok(await _trackingDetailService.GetAllByTrackingIdAsync(trackingId: trackingId));
    }
    
    [Authorize]
    [HttpGet(APIRoute.WarehouseTrackingDetail.GetAllNotExistItemByTrackingId, Name = nameof(GetAllNotExistItemDetailByTrackingIdAsync))]
    public async Task<IActionResult> GetAllNotExistItemDetailByTrackingIdAsync([FromRoute] int trackingId)
    {
        return Ok(await _trackingDetailService.GetAllNotExistItemByTrackingIdAsync(trackingId: trackingId));
    }
}