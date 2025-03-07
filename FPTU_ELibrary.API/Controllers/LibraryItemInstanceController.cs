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
    [HttpGet(APIRoute.LibraryItemInstance.GetByBarcodeToConfirmUpdateShelf, Name = nameof(GetByBarcodeToConfirmUpdateShelfAsync))]
    public async Task<IActionResult> GetByBarcodeToConfirmUpdateShelfAsync([FromRoute] string barcode)
    {
        return Ok(await _itemInstanceService.GetByBarcodeToConfirmUpdateShelfAsync(barcode: barcode));
    }
    
    [Authorize]
    [HttpGet(APIRoute.LibraryItemInstance.GenerateBarcodeRange, Name = nameof(GenerateBarcodeRangeAsync))]
    public async Task<IActionResult> GenerateBarcodeRangeAsync([FromQuery] int categoryId,
        [FromQuery] int totalItem, [FromQuery] int? skipItem = 0)
    {
        return Ok(await _itemInstanceService.GenerateBarcodeRangeAsync(
            categoryId: categoryId,
            totalItem: totalItem,
            skipItem: skipItem));
    }
    
    [Authorize]
    [HttpGet(APIRoute.LibraryItemInstance.CheckExistBarcode, Name = nameof(CheckExistLibraryItemInstanceByBarcodeAsync))]
    public async Task<IActionResult> CheckExistLibraryItemInstanceByBarcodeAsync([FromQuery] string barcode)
    {
        return Ok(await _itemInstanceService.CheckExistBarcodeAsync(barcode: barcode));
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
    [HttpPatch(APIRoute.LibraryItemInstance.UpdateRangeInShelf, Name = nameof(UpdateRangeItemInstanceInShelfAsync))]
    public async Task<IActionResult> UpdateRangeItemInstanceInShelfAsync([FromBody] UpdateRangeItemInstanceShelfRequest req)
    {
        return Ok(await _itemInstanceService.UpdateRangeInShelfAsync(barcodes: req.Barcodes));
    }
    
    [Authorize]
    [HttpPatch(APIRoute.LibraryItemInstance.UpdateRangeOutOfShelf, Name = nameof(UpdateRangeItemInstanceOutOfShelfAsync))]
    public async Task<IActionResult> UpdateRangeItemInstanceOutOfShelfAsync([FromBody] UpdateRangeItemInstanceShelfRequest req)
    {
        return Ok(await _itemInstanceService.UpdateRangeOutOfShelfAsync(barcodes: req.Barcodes));
    }
    
    [Authorize]
    [HttpPatch(APIRoute.LibraryItemInstance.UpdateInShelf, Name = nameof(UpdateItemInstanceInShelfAsync))]
    public async Task<IActionResult> UpdateItemInstanceInShelfAsync([FromRoute] string barcode)
    {
        return Ok(await _itemInstanceService.UpdateInShelfAsync(barcode: barcode));
    }
    
    [Authorize]
    [HttpPatch(APIRoute.LibraryItemInstance.UpdateOutOfShelf, Name = nameof(UpdateItemInstanceOutOfShelfAsync))]
    public async Task<IActionResult> UpdateItemInstanceOutOfShelfAsync([FromRoute] string barcode)
    {
        return Ok(await _itemInstanceService.UpdateOutOfShelfAsync(barcode: barcode));
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