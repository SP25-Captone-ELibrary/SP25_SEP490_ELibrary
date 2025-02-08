using FPTU_ELibrary.API.Extensions;
using FPTU_ELibrary.API.Payloads;
using FPTU_ELibrary.API.Payloads.Requests;
using FPTU_ELibrary.API.Payloads.Requests.LibraryItemInstance;
using FPTU_ELibrary.Application.Dtos.LibraryItems;
using FPTU_ELibrary.Domain.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FPTU_ELibrary.API.Controllers;

[ApiController]
public class LibraryItemInstanceController : ControllerBase
{
    private readonly ILibraryItemInstanceService<LibraryItemInstanceDto> _itemInstanceService;
    
    public LibraryItemInstanceController(
        ILibraryItemInstanceService<LibraryItemInstanceDto> itemInstanceService)
    {
        _itemInstanceService = itemInstanceService;
    }
    
    [Authorize]
    [HttpGet(APIRoute.LibraryItemInstance.GetById, Name = nameof(GetLibraryItemInstanceByIdAsync))]
    public async Task<IActionResult> GetLibraryItemInstanceByIdAsync([FromRoute] int id)
    {
        return Ok(await _itemInstanceService.GetByIdAsync(id));
    }
    
    [Authorize]
    [HttpGet(APIRoute.LibraryItemInstance.GetByBarcode, Name = nameof(GetLibraryItemInstanceByBarcodeAsync))]
    public async Task<IActionResult> GetLibraryItemInstanceByBarcodeAsync([FromQuery] string barcode)
    {
        return Ok(await _itemInstanceService.GetByBarcodeAsync(barcode: barcode));
    }
    
    [Authorize]
    [HttpPost(APIRoute.LibraryItemInstance.AddRange, Name = nameof(AddRangeCopyToBookEditionAsync))]
    public async Task<IActionResult> AddRangeCopyToBookEditionAsync(
        [FromRoute] int id, 
        [FromBody] CreateRangeItemInstanceRequest req)
    {
        return Ok(await _itemInstanceService.AddRangeToLibraryItemAsync(id, req.ToListLibraryItemInstanceDto()));
    }
    
    [Authorize]
    [HttpPut(APIRoute.LibraryItemInstance.Update, Name = nameof(UpdateItemInstanceAsync))]
    public async Task<IActionResult> UpdateItemInstanceAsync([FromRoute] int id, [FromBody] UpdateItemInstanceRequest req)
    {
        return Ok(await _itemInstanceService.UpdateAsync(id, req.ToLibraryItemInstanceDto()));
    }
    
    [Authorize]
    [HttpPut(APIRoute.LibraryItemInstance.UpdateRange, Name = nameof(UpdateRangeEditionCopyAsync))]
    public async Task<IActionResult> UpdateRangeEditionCopyAsync(
        [FromRoute] int libraryItemId, 
        [FromBody] UpdateRangeItemInstanceRequest req)
    {
         return Ok(await _itemInstanceService.UpdateRangeAsync(libraryItemId, req.ToListLibraryItemInstanceDto()));
    }
    
    [Authorize]
    [HttpPatch(APIRoute.LibraryItemInstance.SoftDelete, Name = nameof(SoftDeleteItemInstanceAsync))]
    public async Task<IActionResult> SoftDeleteItemInstanceAsync([FromRoute] int id)
    {
        return Ok(await _itemInstanceService.SoftDeleteAsync(id));
    }
    
    [Authorize]
    [HttpPatch(APIRoute.LibraryItemInstance.SoftDeleteRange, Name = nameof(SoftDeleteRangeItemInstanceAsync))]
    public async Task<IActionResult> SoftDeleteRangeItemInstanceAsync([FromRoute] int libraryItemId, [FromBody] RangeRequest<int> req)
    {
        return Ok(await _itemInstanceService.SoftDeleteRangeAsync(libraryItemId, req.Ids.ToList()));
    }
    
    [Authorize]
    [HttpPatch(APIRoute.LibraryItemInstance.UndoDelete, Name = nameof(UndoDeleteItemInstanceAsync))]
    public async Task<IActionResult> UndoDeleteItemInstanceAsync([FromRoute] int id)
    {
        return Ok(await _itemInstanceService.UndoDeleteAsync(id));
    }
    
    [Authorize]
    [HttpPatch(APIRoute.LibraryItemInstance.UndoDeleteRange, Name = nameof(UndoDeleteRangeItemInstanceAsync))]
    public async Task<IActionResult> UndoDeleteRangeItemInstanceAsync([FromRoute] int libraryItemId, [FromBody] RangeRequest<int> req)
    {
        return Ok(await _itemInstanceService.UndoDeleteRangeAsync(libraryItemId, req.Ids.ToList()));
    }
    
    [Authorize]
    [HttpDelete(APIRoute.LibraryItemInstance.Delete, Name = nameof(DeleteEditionCopyAsync))]
    public async Task<IActionResult> DeleteEditionCopyAsync([FromRoute] int id)
    {
        return Ok(await _itemInstanceService.DeleteAsync(id));
    }
    
    [Authorize]
    [HttpDelete(APIRoute.LibraryItemInstance.DeleteRange, Name = nameof(DeleteRangeItemInstanceAsync))]
    public async Task<IActionResult> DeleteRangeItemInstanceAsync([FromRoute] int libraryItemId, [FromBody] RangeRequest<int> req)
    {
        return Ok(await _itemInstanceService.DeleteRangeAsync(libraryItemId, req.Ids.ToList()));
    }
}