using FPTU_ELibrary.API.Extensions;
using FPTU_ELibrary.API.Payloads;
using FPTU_ELibrary.API.Payloads.Requests;
using FPTU_ELibrary.API.Payloads.Requests.Book;
using FPTU_ELibrary.API.Payloads.Requests.LibraryItem;
using FPTU_ELibrary.Application.Configurations;
using FPTU_ELibrary.Application.Dtos.LibraryItems;
using FPTU_ELibrary.Domain.Interfaces.Services;
using FPTU_ELibrary.Domain.Specifications;
using FPTU_ELibrary.Domain.Specifications.Params;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace FPTU_ELibrary.API.Controllers;

[ApiController]
public class LibraryItemController : ControllerBase
{
    private readonly AppSettings _appSettings;
    private readonly ILibraryItemService<LibraryItemDto> _libraryItemService;
    private readonly ILibraryItemInstanceService<LibraryItemInstanceDto> _itemInstanceService;
    private readonly ILibraryItemAuthorService<LibraryItemAuthorDto> _itemAuthorService;
    
    public LibraryItemController(
        ILibraryItemService<LibraryItemDto> libraryItemService,
        ILibraryItemInstanceService<LibraryItemInstanceDto> itemInstanceService,
        ILibraryItemAuthorService<LibraryItemAuthorDto> itemAuthorService,
        IOptionsMonitor<AppSettings> monitor)
    {
        _libraryItemService = libraryItemService;
        _itemInstanceService = itemInstanceService;
        _itemAuthorService = itemAuthorService;
        _appSettings = monitor.CurrentValue;
    }

    [Authorize]
    [HttpPost(APIRoute.LibraryItem.AddAuthor, Name = nameof(AddAuthorToLibraryItemAsync))]
    public async Task<IActionResult> AddAuthorToLibraryItemAsync([FromBody] AddAuthorRequest req)
    {
        return Ok(await _itemAuthorService.AddAuthorToLibraryItemAsync(req.LibraryItemId, req.AuthorId));
    }
    
    
    [Authorize]
    [HttpPost(APIRoute.LibraryItem.AddRangeAuthor, Name = nameof(AddRangeAuthorToLibraryItemAsync))]
    public async Task<IActionResult> AddRangeAuthorToLibraryItemAsync([FromBody] AddRangeAuthorRequest req)
    {
        return Ok(await _itemAuthorService.AddRangeAuthorToLibraryItemAsync(req.LibraryItemId,
            req.AuthorIds.ToArray()));
    }
    
    
    [Authorize]
    [HttpDelete(APIRoute.LibraryItem.DeleteAuthor, Name = nameof(DeleteAuthorFromLibraryItemAsync))]
    public async Task<IActionResult> DeleteAuthorFromLibraryItemAsync([FromBody] DeleteAuthorRequest req)
    {
        return Ok(await _itemAuthorService.DeleteAuthorFromLibraryItemAsync(req.LibraryItemId, req.AuthorId));
    }
    
    [Authorize]
    [HttpDelete(APIRoute.LibraryItem.DeleteRangeAuthor, Name = nameof(DeleteRangeAuthorFromLibraryItemAsync))]
    public async Task<IActionResult> DeleteRangeAuthorFromLibraryItemAsync([FromBody] DeleteRangeAuthorRequest req)
    {
        return Ok(await _itemAuthorService.DeleteRangeAuthorFromLibraryItemAsync(req.LibraryItemId,
            req.AuthorIds.ToArray()));
    }
    
    [Authorize]
    [HttpPost(APIRoute.LibraryItem.Create, Name = nameof(CreateLibraryItemAsync))]
    public async Task<IActionResult> CreateLibraryItemAsync([FromBody] CreateLibraryItemRequest req)
    {
        return Ok(await _libraryItemService.CreateAsync(req.ToLibraryItemDto()));
    }
    
    [Authorize]
    [HttpGet(APIRoute.LibraryItem.GetAll, Name = nameof(GetAllBookEditionAsync))]
    public async Task<IActionResult> GetAllBookEditionAsync([FromQuery] LibraryItemSpecParams specParams)
    {
        return Ok(await _libraryItemService.GetAllWithSpecAsync(new LibraryItemSpecification(
            specParams: specParams,
            pageIndex: specParams.PageIndex ?? 1,
            pageSize: specParams.PageSize ?? _appSettings.PageSize), tracked: false));
    }
    
    [Authorize]
    [HttpGet(APIRoute.LibraryItem.GetDetail, Name = nameof(GetLibraryItemByIdAsync))]
    public async Task<IActionResult> GetLibraryItemByIdAsync([FromRoute] int id)
    {
        return Ok(await _libraryItemService.GetDetailAsync(id));
    }    
    
    [Authorize]
    [HttpGet(APIRoute.LibraryItem.CountTotalInstance, Name = nameof(CountTotalInstanceAsync))]
    public async Task<IActionResult> CountTotalInstanceAsync([FromRoute] int id)
    {
        return Ok(await _itemInstanceService.CountTotalItemInstanceAsync(libraryItemId: id));
    }
    
    [Authorize]
    [HttpGet(APIRoute.LibraryItem.CountRangeTotalInstance, Name = nameof(CountRangeTotalInstanceAsync))]
    public async Task<IActionResult> CountRangeTotalInstanceAsync([FromBody] RangeRequest<int> req)
    {
        return Ok(await _itemInstanceService.CountTotalItemInstanceAsync(libraryItemIds: req.Ids.ToList()));
    }
    
    //
    // [Authorize]
    // [HttpPut(APIRoute.BookEdition.Update, Name = nameof(UpdateBookEditionAsync))]
    // public async Task<IActionResult> UpdateBookEditionAsync([FromRoute] int id, [FromBody] UpdateBookEditionRequest req)
    // {
    //     return Ok(await _libraryItemService.UpdateAsync(id, req.ToBookEditionDto()));
    // }
    //
    // [Authorize]
    // [HttpPatch(APIRoute.BookEdition.UpdateStatus, Name = nameof(UpdateBookEditionStatusAsync))]
    // public async Task<IActionResult> UpdateBookEditionStatusAsync([FromRoute] int id)
    // {
    //     return Ok(await _libraryItemService.UpdateStatusAsync(id));
    // }
    //
    
    [Authorize]
    [HttpPatch(APIRoute.LibraryItem.UpdateShelfLocation, Name = nameof(UpdateBookEditionShelfLocationAsync))]
    public async Task<IActionResult> UpdateBookEditionShelfLocationAsync([FromRoute] int id, [FromQuery] int? shelfId)
    {
        return Ok(await _libraryItemService.UpdateShelfLocationAsync(id, shelfId));
    }
    
    // [Authorize]
    // [HttpPatch(APIRoute.BookEdition.SoftDelete, Name = nameof(SoftDeleteBookEditionAsync))]
    // public async Task<IActionResult> SoftDeleteBookEditionAsync([FromRoute] int id)
    // {
    //     return Ok(await _libraryItemService.SoftDeleteAsync(id));
    // }
    //
    // [Authorize]
    // [HttpPatch(APIRoute.BookEdition.SoftDeleteRange, Name = nameof(SoftDeleteRangeBookEditionAsync))]
    // public async Task<IActionResult> SoftDeleteRangeBookEditionAsync([FromBody] RangeRequest<int> req)
    // {
    //     return Ok(await _libraryItemService.SoftDeleteRangeAsync(req.Ids));
    // }
    //
    // [Authorize]
    // [HttpPatch(APIRoute.BookEdition.UndoDelete, Name = nameof(UndoDeleteBookEditionAsync))]
    // public async Task<IActionResult> UndoDeleteBookEditionAsync([FromRoute] int id)
    // {
    //     return Ok(await _libraryItemService.UndoDeleteAsync(id));
    // }
    //
    // [Authorize]
    // [HttpPatch(APIRoute.BookEdition.UndoDeleteRange, Name = nameof(UndoDeleteRangeBookEditionAsync))]
    // public async Task<IActionResult> UndoDeleteRangeBookEditionAsync([FromBody] RangeRequest<int> req)
    // {
    //     return Ok(await _libraryItemService.UndoDeleteRangeAsync(req.Ids));
    // }
    //
    // [Authorize]
    // [HttpDelete(APIRoute.BookEdition.Delete, Name = nameof(DeleteBookEditionAsync))]
    // public async Task<IActionResult> DeleteBookEditionAsync([FromRoute] int id)
    // {
    //     return Ok(await _libraryItemService.DeleteAsync(id));
    // }
    //
    // [Authorize]
    // [HttpDelete(APIRoute.BookEdition.DeleteRange, Name = nameof(DeleteRangeBookEditionAsync))]
    // public async Task<IActionResult> DeleteRangeBookEditionAsync([FromBody] RangeRequest<int> req)
    // {
    //     return Ok(await _libraryItemService.DeleteRangeAsync(req.Ids));
    // }
    //
    // [Authorize]
    // [HttpPost(APIRoute.BookEdition.Import, Name = nameof(ImportBookEditionAsync))]
    // public async Task<IActionResult> ImportBookEditionAsync([FromForm] ImportBookEditionRequest req)
    // {
    //    return Ok(await _libraryItemService.ImportAsync(
    //        file: req.File,
    //        coverImageFiles: req.CoverImageFiles,
    //        scanningFields: req.ScanningFields));
    // }
    //
    // [Authorize]
    // [HttpGet(APIRoute.BookEdition.Export, Name = nameof(ExportBookEditionAsync))]
    // public async Task<IActionResult> ExportBookEditionAsync([FromQuery] LibraryItemSpecParams specParams)
    // {
    //     var exportResult = await _libraryItemService.ExportAsync(new LibraryItemSpecification(
    //         specParams: specParams,
    //         pageIndex: specParams.PageIndex ?? 1,
    //         pageSize: specParams.PageSize ?? _appSettings.PageSize));
    //
    //     return exportResult.Data is byte[] fileStream
    //         ? File(fileStream, @"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Books.xlsx")
    //         : Ok(exportResult);
    // }
}