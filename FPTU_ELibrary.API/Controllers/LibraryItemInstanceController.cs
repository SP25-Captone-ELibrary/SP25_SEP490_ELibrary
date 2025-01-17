using FPTU_ELibrary.API.Extensions;
using FPTU_ELibrary.API.Payloads;
using FPTU_ELibrary.API.Payloads.Requests;
using FPTU_ELibrary.API.Payloads.Requests.Book;
using FPTU_ELibrary.Application.Dtos.LibraryItems;
using FPTU_ELibrary.Domain.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FPTU_ELibrary.API.Controllers;

[ApiController]
public class LibraryItemInstanceController : ControllerBase
{
    // private readonly ILibraryItemInstanceService<LibraryItemInstanceDto> _editionCopyService;
    //
    // public LibraryItemInstanceController(
    //     ILibraryItemInstanceService<LibraryItemInstanceDto> editionCopyService)
    // {
    //     _editionCopyService = editionCopyService;
    // }
    //
    // [Authorize]
    // [HttpGet(APIRoute.LibraryItemInstance.GetById, Name = nameof(GetEditionCopyByIdAsync))]
    // public async Task<IActionResult> GetEditionCopyByIdAsync([FromRoute] int id)
    // {
    //     return Ok(await _editionCopyService.GetByIdAsync(id));
    // }
    //
    // [Authorize]
    // [HttpPost(APIRoute.LibraryItemInstance.AddRange, Name = nameof(AddRangeCopyToBookEditionAsync))]
    // public async Task<IActionResult> AddRangeCopyToBookEditionAsync(
    //     [FromRoute] int id, 
    //     [FromBody] CreateRangeBookEditionCopyRequest req)
    // {
    //     return Ok(await _editionCopyService.AddRangeToBookEditionAsync(id, req.ToListBookEditionCopyDto()));
    // }
    //
    // [Authorize]
    // [HttpPut(APIRoute.LibraryItemInstance.Update, Name = nameof(UpdateEditionCopyAsync))]
    // public async Task<IActionResult> UpdateEditionCopyAsync([FromRoute] int id, [FromBody] UpdateBookEditionCopyRequest req)
    // {
    //     return Ok(await _editionCopyService.UpdateAsync(id, req.ToBookEditionCopyDto()));
    // }
    //
    // [Authorize]
    // [HttpPut(APIRoute.LibraryItemInstance.UpdateRange, Name = nameof(UpdateRangeEditionCopyAsync))]
    // public async Task<IActionResult> UpdateRangeEditionCopyAsync(
    //     [FromRoute] int bookEditionId, 
    //     [FromBody] UpdateRangeBookEditionCopyRequest req)
    // {
    //      return Ok(await _editionCopyService.UpdateRangeAsync(bookEditionId, req.BookEditionCopyIds, req.Status));
    // }
    //
    // [Authorize]
    // [HttpPatch(APIRoute.LibraryItemInstance.SoftDelete, Name = nameof(SoftDeleteEditionCopyAsync))]
    // public async Task<IActionResult> SoftDeleteEditionCopyAsync([FromRoute] int id)
    // {
    //     return Ok(await _editionCopyService.SoftDeleteAsync(id));
    // }
    //
    // [Authorize]
    // [HttpPatch(APIRoute.LibraryItemInstance.SoftDeleteRange, Name = nameof(SoftDeleteRangeEditionCopyAsync))]
    // public async Task<IActionResult> SoftDeleteRangeEditionCopyAsync([FromRoute] int bookEditionId, [FromBody] RangeRequest<int> req)
    // {
    //     return Ok(await _editionCopyService.SoftDeleteRangeAsync(bookEditionId, req.Ids.ToList()));
    // }
    //
    // [Authorize]
    // [HttpPatch(APIRoute.LibraryItemInstance.UndoDelete, Name = nameof(UndoDeleteEditionCopyAsync))]
    // public async Task<IActionResult> UndoDeleteEditionCopyAsync([FromRoute] int id)
    // {
    //     return Ok(await _editionCopyService.UndoDeleteAsync(id));
    // }
    //
    // [Authorize]
    // [HttpPatch(APIRoute.LibraryItemInstance.UndoDeleteRange, Name = nameof(UndoDeleteRangeEditionCopyAsync))]
    // public async Task<IActionResult> UndoDeleteRangeEditionCopyAsync([FromRoute] int bookEditionId, [FromBody] RangeRequest<int> req)
    // {
    //     return Ok(await _editionCopyService.UndoDeleteRangeAsync(bookEditionId, req.Ids.ToList()));
    // }
    //
    // [Authorize]
    // [HttpDelete(APIRoute.LibraryItemInstance.Delete, Name = nameof(DeleteEditionCopyAsync))]
    // public async Task<IActionResult> DeleteEditionCopyAsync([FromRoute] int id)
    // {
    //     return Ok(await _editionCopyService.DeleteAsync(id));
    // }
    //
    // [Authorize]
    // [HttpDelete(APIRoute.LibraryItemInstance.DeleteRange, Name = nameof(DeleteRangeEditionCopyAsync))]
    // public async Task<IActionResult> DeleteRangeEditionCopyAsync([FromRoute] int bookEditionId, [FromBody] RangeRequest<int> req)
    // {
    //     return Ok(await _editionCopyService.DeleteRangeAsync(bookEditionId, req.Ids.ToList()));
    // }
}