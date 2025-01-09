using FPTU_ELibrary.API.Extensions;
using FPTU_ELibrary.API.Payloads;
using FPTU_ELibrary.API.Payloads.Requests;
using FPTU_ELibrary.API.Payloads.Requests.Book;
using FPTU_ELibrary.API.Payloads.Requests.BookEdition;
using FPTU_ELibrary.Application.Configurations;
using FPTU_ELibrary.Application.Dtos.BookEditions;
using FPTU_ELibrary.Domain.Interfaces.Services;
using FPTU_ELibrary.Domain.Specifications;
using FPTU_ELibrary.Domain.Specifications.Params;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace FPTU_ELibrary.API.Controllers;

[ApiController]
public class BookEditionController : ControllerBase
{
    private readonly AppSettings _appSettings;
    private readonly IBookEditionService<BookEditionDto> _bookEditionService;
    private readonly IBookEditionCopyService<BookEditionCopyDto> _editionCopyService;
    private readonly IBookEditionAuthorService<BookEditionAuthorDto> _editionAuthorService;

    public BookEditionController(
        IBookEditionService<BookEditionDto> bookEditionService,
        IBookEditionCopyService<BookEditionCopyDto> editionCopyService,
        IBookEditionAuthorService<BookEditionAuthorDto> editionAuthorService,
        IOptionsMonitor<AppSettings> monitor)
    {
        _bookEditionService = bookEditionService;
        _editionCopyService = editionCopyService;
        _editionAuthorService = editionAuthorService;
        _appSettings = monitor.CurrentValue;
    }

    [Authorize]
    [HttpPost(APIRoute.BookEdition.AddAuthor, Name = nameof(AddAuthorToBookEditionAsync))]
    public async Task<IActionResult> AddAuthorToBookEditionAsync([FromBody] AddAuthorRequest req)
    {
        return Ok(await _editionAuthorService.AddAuthorToBookEditionAsync(req.BookEditionId, req.AuthorId));
    }

    [Authorize]
    [HttpPost(APIRoute.BookEdition.AddRangeAuthor, Name = nameof(AddRangeAuthorToBookEditionAsync))]
    public async Task<IActionResult> AddRangeAuthorToBookEditionAsync([FromBody] AddRangeAuthorRequest req)
    {
        return Ok(await _editionAuthorService.AddRangeAuthorToBookEditionAsync(req.BookEditionId,
            req.AuthorIds.ToArray()));
    }
    
    [Authorize]
    [HttpDelete(APIRoute.BookEdition.DeleteAuthor, Name = nameof(DeleteAuthorToBookEditionAsync))]
    public async Task<IActionResult> DeleteAuthorToBookEditionAsync([FromBody] DeleteAuthorRequest req)
    {
        return Ok(await _editionAuthorService.DeleteAuthorFromBookEditionAsync(req.BookEditionId, req.AuthorId));
    }
    
    [Authorize]
    [HttpDelete(APIRoute.BookEdition.DeleteRangeAuthor, Name = nameof(DeleteRangeAuthorToBookEditionAsync))]
    public async Task<IActionResult> DeleteRangeAuthorToBookEditionAsync([FromBody] DeleteRangeAuthorRequest req)
    {
        return Ok(await _editionAuthorService.DeleteRangeAuthorFromBookEditionAsync(req.BookEditionId,
            req.AuthorIds.ToArray()));
    }
    
    [Authorize]
    [HttpPost(APIRoute.BookEdition.Create, Name = nameof(CreateBookEditionAsync))]
    public async Task<IActionResult> CreateBookEditionAsync([FromRoute] int bookId, [FromBody] CreateBookEditionRequest req)
    {
        return Ok(await _bookEditionService.CreateAsync(bookId, req.ToBookEditionDto()));
    }
    
    [Authorize]
    [HttpGet(APIRoute.BookEdition.GetAllEdition, Name = nameof(GetAllBookEditionAsync))]
    public async Task<IActionResult> GetAllBookEditionAsync([FromQuery] BookEditionSpecParams specParams)
    {
        return Ok(await _bookEditionService.GetAllWithSpecAsync(new BookEditionSpecification(
            specParams: specParams,
            pageIndex: specParams.PageIndex ?? 1,
            pageSize: specParams.PageSize ?? _appSettings.PageSize), tracked: false));
    }
    
    [Authorize]
    [HttpGet(APIRoute.BookEdition.GetEditionById, Name = nameof(GetBookEditionByIdAsync))]
    public async Task<IActionResult> GetBookEditionByIdAsync([FromRoute] int id)
    {
        return Ok(await _bookEditionService.GetDetailAsync(id));
    }    
    
    [Authorize]
    [HttpGet(APIRoute.BookEdition.CountTotalCopy, Name = nameof(CountTotalCopyAsync))]
    public async Task<IActionResult> CountTotalCopyAsync([FromRoute] int id)
    {
        return Ok(await _editionCopyService.CountTotalEditionCopyAsync(bookEditionId: id));
    }
    
    [Authorize]
    [HttpGet(APIRoute.BookEdition.CountRangeTotalCopy, Name = nameof(CountRangeTotalCopyAsync))]
    public async Task<IActionResult> CountRangeTotalCopyAsync([FromBody] RangeRequest<int> req)
    {
        return Ok(await _editionCopyService.CountTotalEditionCopyAsync(bookEditionIds: req.Ids.ToList()));
    }

    [Authorize]
    [HttpPut(APIRoute.BookEdition.Update, Name = nameof(UpdateBookEditionAsync))]
    public async Task<IActionResult> UpdateBookEditionAsync([FromRoute] int id, [FromBody] UpdateBookEditionRequest req)
    {
        return Ok(await _bookEditionService.UpdateAsync(id, req.ToBookEditionDto()));
    }
    
    [Authorize]
    [HttpPatch(APIRoute.BookEdition.UpdateStatus, Name = nameof(UpdateBookEditionStatusAsync))]
    public async Task<IActionResult> UpdateBookEditionStatusAsync([FromRoute] int id)
    {
        return Ok(await _bookEditionService.UpdateStatusAsync(id));
    }
    
    [Authorize]
    [HttpPatch(APIRoute.BookEdition.UpdateShelfLocation, Name = nameof(UpdateBookEditionShelfLocationAsync))]
    public async Task<IActionResult> UpdateBookEditionShelfLocationAsync([FromRoute] int id, [FromQuery] int? shelfId)
    {
        return Ok(await _bookEditionService.UpdateShelfLocationAsync(id, shelfId));
    }
    
    [Authorize]
    [HttpPatch(APIRoute.BookEdition.SoftDelete, Name = nameof(SoftDeleteBookEditionAsync))]
    public async Task<IActionResult> SoftDeleteBookEditionAsync([FromRoute] int id)
    {
        return Ok(await _bookEditionService.SoftDeleteAsync(id));
    }
    
    [Authorize]
    [HttpPatch(APIRoute.BookEdition.SoftDeleteRange, Name = nameof(SoftDeleteRangeBookEditionAsync))]
    public async Task<IActionResult> SoftDeleteRangeBookEditionAsync([FromBody] RangeRequest<int> req)
    {
        return Ok(await _bookEditionService.SoftDeleteRangeAsync(req.Ids));
    }
    
    [Authorize]
    [HttpPatch(APIRoute.BookEdition.UndoDelete, Name = nameof(UndoDeleteBookEditionAsync))]
    public async Task<IActionResult> UndoDeleteBookEditionAsync([FromRoute] int id)
    {
        return Ok(await _bookEditionService.UndoDeleteAsync(id));
    }
    
    [Authorize]
    [HttpPatch(APIRoute.BookEdition.UndoDeleteRange, Name = nameof(UndoDeleteRangeBookEditionAsync))]
    public async Task<IActionResult> UndoDeleteRangeBookEditionAsync([FromBody] RangeRequest<int> req)
    {
        return Ok(await _bookEditionService.UndoDeleteRangeAsync(req.Ids));
    }
    
    [Authorize]
    [HttpDelete(APIRoute.BookEdition.Delete, Name = nameof(DeleteBookEditionAsync))]
    public async Task<IActionResult> DeleteBookEditionAsync([FromRoute] int id)
    {
        return Ok(await _bookEditionService.DeleteAsync(id));
    }
    
    [Authorize]
    [HttpDelete(APIRoute.BookEdition.DeleteRange, Name = nameof(DeleteRangeBookEditionAsync))]
    public async Task<IActionResult> DeleteRangeBookEditionAsync([FromBody] RangeRequest<int> req)
    {
        return Ok(await _bookEditionService.DeleteRangeAsync(req.Ids));
    }

    [Authorize]
    [HttpPost(APIRoute.BookEdition.Import, Name = nameof(ImportBookEditionAsync))]
    public async Task<IActionResult> ImportBookEditionAsync([FromForm] ImportBookEditionRequest req)
    {
       return Ok(await _bookEditionService.ImportAsync(
           file: req.File,
           coverImageFiles: req.CoverImageFiles,
           scanningFields: req.ScanningFields));
    }
    
    [Authorize]
    [HttpGet(APIRoute.BookEdition.Export, Name = nameof(ExportBookEditionAsync))]
    public async Task<IActionResult> ExportBookEditionAsync([FromQuery] BookEditionSpecParams specParams)
    {
        var exportResult = await _bookEditionService.ExportAsync(new BookEditionSpecification(
            specParams: specParams,
            pageIndex: specParams.PageIndex ?? 1,
            pageSize: specParams.PageSize ?? _appSettings.PageSize));
    
        return exportResult.Data is byte[] fileStream
            ? File(fileStream, @"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Books.xlsx")
            : Ok(exportResult);
    }
}