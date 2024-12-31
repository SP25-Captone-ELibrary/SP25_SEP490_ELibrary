using FPTU_ELibrary.API.Extensions;
using FPTU_ELibrary.API.Payloads;
using FPTU_ELibrary.API.Payloads.Requests;
using FPTU_ELibrary.API.Payloads.Requests.Book;
using FPTU_ELibrary.Application.Dtos.BookEditions;
using FPTU_ELibrary.Domain.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FPTU_ELibrary.API.Controllers;

[ApiController]
public class BookEditionCopyController : ControllerBase
{
    private readonly IBookEditionCopyService<BookEditionCopyDto> _editionCopyService;

    public BookEditionCopyController(
        IBookEditionCopyService<BookEditionCopyDto> editionCopyService)
    {
        _editionCopyService = editionCopyService;
    }

    [Authorize]
    [HttpGet(APIRoute.BookEditionCopy.GetById, Name = nameof(GetEditionCopyByIdAsync))]
    public async Task<IActionResult> GetEditionCopyByIdAsync([FromRoute] int id)
    {
        return Ok(await _editionCopyService.GetByIdAsync(id));
    }
    
    [Authorize]
    [HttpPost(APIRoute.BookEditionCopy.AddRange, Name = nameof(AddRangeCopyToBookEditionAsync))]
    public async Task<IActionResult> AddRangeCopyToBookEditionAsync(
        [FromRoute] int id, 
        [FromBody] CreateRangeBookEditionCopyRequest req)
    {
        return Ok(await _editionCopyService.AddRangeToBookEditionAsync(id, req.Codes));
    }

    [Authorize]
    [HttpPut(APIRoute.BookEditionCopy.Update, Name = nameof(UpdateEditionCopyAsync))]
    public async Task<IActionResult> UpdateEditionCopyAsync([FromRoute] int id, [FromBody] UpdateBookEditionCopyRequest req)
    {
        return Ok(await _editionCopyService.UpdateAsync(id, req.ToBookEditionCopyDto()));
    }
    
    [Authorize]
    [HttpPut(APIRoute.BookEditionCopy.UpdateRange, Name = nameof(UpdateRangeEditionCopyAsync))]
    public async Task<IActionResult> UpdateRangeEditionCopyAsync(
        [FromRoute] int bookEditionId, 
        [FromBody] UpdateRangeBookEditionCopyRequest req)
    {
         return Ok(await _editionCopyService.UpdateRangeAsync(bookEditionId, req.BookEditionCopyIds, req.Status));
    }
    
    [Authorize]
    [HttpPatch(APIRoute.BookEditionCopy.SoftDelete, Name = nameof(SoftDeleteEditionCopyAsync))]
    public async Task<IActionResult> SoftDeleteEditionCopyAsync([FromRoute] int id)
    {
        return Ok(await _editionCopyService.SoftDeleteAsync(id));
    }
    
    [Authorize]
    [HttpPatch(APIRoute.BookEditionCopy.SoftDeleteRange, Name = nameof(SoftDeleteRangeEditionCopyAsync))]
    public async Task<IActionResult> SoftDeleteRangeEditionCopyAsync([FromRoute] int bookEditionId, [FromBody] RangeRequest<int> req)
    {
        return Ok(await _editionCopyService.SoftDeleteRangeAsync(bookEditionId, req.Ids.ToList()));
    }
    
    [Authorize]
    [HttpPatch(APIRoute.BookEditionCopy.UndoDelete, Name = nameof(UndoDeleteEditionCopyAsync))]
    public async Task<IActionResult> UndoDeleteEditionCopyAsync([FromRoute] int id)
    {
        return Ok(await _editionCopyService.UndoDeleteAsync(id));
    }
    
    [Authorize]
    [HttpPatch(APIRoute.BookEditionCopy.UndoDeleteRange, Name = nameof(UndoDeleteRangeEditionCopyAsync))]
    public async Task<IActionResult> UndoDeleteRangeEditionCopyAsync([FromRoute] int bookEditionId, [FromBody] RangeRequest<int> req)
    {
        return Ok(await _editionCopyService.UndoDeleteRangeAsync(bookEditionId, req.Ids.ToList()));
    }
    
    [Authorize]
    [HttpDelete(APIRoute.BookEditionCopy.Delete, Name = nameof(DeleteEditionCopyAsync))]
    public async Task<IActionResult> DeleteEditionCopyAsync([FromRoute] int id)
    {
        return Ok(await _editionCopyService.DeleteAsync(id));
    }
    
    [Authorize]
    [HttpDelete(APIRoute.BookEditionCopy.DeleteRange, Name = nameof(DeleteRangeEditionCopyAsync))]
    public async Task<IActionResult> DeleteRangeEditionCopyAsync([FromRoute] int bookEditionId, [FromBody] RangeRequest<int> req)
    {
        return Ok(await _editionCopyService.DeleteRangeAsync(bookEditionId, req.Ids.ToList()));
    }
}