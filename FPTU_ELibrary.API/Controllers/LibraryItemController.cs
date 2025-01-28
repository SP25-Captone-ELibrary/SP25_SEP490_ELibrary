using FPTU_ELibrary.API.Extensions;
using FPTU_ELibrary.API.Payloads;
using FPTU_ELibrary.API.Payloads.Filters;
using FPTU_ELibrary.API.Payloads.Requests;
using FPTU_ELibrary.API.Payloads.Requests.LibraryItem;
using FPTU_ELibrary.API.Payloads.Requests.OCR;
using FPTU_ELibrary.Application.Configurations;
using FPTU_ELibrary.Application.Dtos.Authors;
using FPTU_ELibrary.Application.Dtos.LibraryItems;
using FPTU_ELibrary.Application.Elastic.Params;
using FPTU_ELibrary.Application.Services.IServices;
using FPTU_ELibrary.Domain.Interfaces.Services;
using FPTU_ELibrary.Domain.Interfaces.Services.Base;
using FPTU_ELibrary.Domain.Specifications;
using FPTU_ELibrary.Domain.Specifications.Params;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Nest;

namespace FPTU_ELibrary.API.Controllers;

[ApiController]
public class LibraryItemController : ControllerBase
{
    private readonly AppSettings _appSettings;
    private readonly IAuthorService<AuthorDto> _authorService;
    private readonly ILibraryItemService<LibraryItemDto> _libraryItemService;
    private readonly ILibraryItemInstanceService<LibraryItemInstanceDto> _itemInstanceService;
    private readonly ILibraryItemAuthorService<LibraryItemAuthorDto> _itemAuthorService;
    private readonly IAIDetectionService _aiDetectionService;

    private readonly ISearchService _searchService;

    public LibraryItemController(
        IAuthorService<AuthorDto> authorService,
        ILibraryItemService<LibraryItemDto> libraryItemService,
        ILibraryItemInstanceService<LibraryItemInstanceDto> itemInstanceService,
        ILibraryItemAuthorService<LibraryItemAuthorDto> itemAuthorService,
        IAIDetectionService aiDetectionService,
        ISearchService searchService,
        IOptionsMonitor<AppSettings> monitor)
    {
        _authorService = authorService;
        _libraryItemService = libraryItemService;
        _itemInstanceService = itemInstanceService;
        _itemAuthorService = itemAuthorService;
        _aiDetectionService = aiDetectionService;
        _searchService = searchService;
        _appSettings = monitor.CurrentValue;
    }

    #region Management

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
    [HttpGet(APIRoute.LibraryItem.GetEnums, Name = nameof(GetLibraryItemEnumsAsync))]
    public async Task<IActionResult> GetLibraryItemEnumsAsync()
    {
        return Ok(await _libraryItemService.GetEnumValueAsync());
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
    [HttpGet(APIRoute.LibraryItem.GetById, Name = nameof(GetLibraryItemByIdAsync))]
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

    [Authorize]
    [HttpPut(APIRoute.LibraryItem.Update, Name = nameof(UpdateLibraryItemAsync))]
    public async Task<IActionResult> UpdateLibraryItemAsync([FromRoute] int id, [FromBody] UpdateLibraryItemRequest req)
    {
        return Ok(await _libraryItemService.UpdateAsync(id, req.ToLibraryItemDto()));
    }

    [Authorize]
    [HttpPatch(APIRoute.LibraryItem.UpdateStatus, Name = nameof(UpdateLibraryItemStatusAsync))]
    public async Task<IActionResult> UpdateLibraryItemStatusAsync([FromRoute] int id)
    {
        return Ok(await _libraryItemService.UpdateStatusAsync(id));
    }

    [Authorize]
    [HttpPatch(APIRoute.LibraryItem.UpdateShelfLocation, Name = nameof(UpdateLibraryItemShelfLocationAsync))]
    public async Task<IActionResult> UpdateLibraryItemShelfLocationAsync([FromRoute] int id, [FromQuery] int? shelfId)
    {
        return Ok(await _libraryItemService.UpdateShelfLocationAsync(id, shelfId));
    }

    [Authorize]
    [HttpPatch(APIRoute.LibraryItem.SoftDelete, Name = nameof(SoftDeleteLibraryItemAsync))]
    public async Task<IActionResult> SoftDeleteLibraryItemAsync([FromRoute] int id)
    {
        return Ok(await _libraryItemService.SoftDeleteAsync(id));
    }

    [Authorize]
    [HttpPatch(APIRoute.LibraryItem.SoftDeleteRange, Name = nameof(SoftDeleteRangeLibraryItemAsync))]
    public async Task<IActionResult> SoftDeleteRangeLibraryItemAsync([FromBody] RangeRequest<int> req)
    {
        return Ok(await _libraryItemService.SoftDeleteRangeAsync(req.Ids));
    }

    [Authorize]
    [HttpPatch(APIRoute.LibraryItem.UndoDelete, Name = nameof(UndoDeleteItemAsync))]
    public async Task<IActionResult> UndoDeleteItemAsync([FromRoute] int id)
    {
        return Ok(await _libraryItemService.UndoDeleteAsync(id));
    }

    [Authorize]
    [HttpPatch(APIRoute.LibraryItem.UndoDeleteRange, Name = nameof(UndoDeleteRangeItemAsync))]
    public async Task<IActionResult> UndoDeleteRangeItemAsync([FromBody] RangeRequest<int> req)
    {
        return Ok(await _libraryItemService.UndoDeleteRangeAsync(req.Ids));
    }

    [Authorize]
    [HttpDelete(APIRoute.LibraryItem.Delete, Name = nameof(DeleteItemAsync))]
    public async Task<IActionResult> DeleteItemAsync([FromRoute] int id)
    {
        return Ok(await _libraryItemService.DeleteAsync(id));
    }

    [Authorize]
    [HttpDelete(APIRoute.LibraryItem.DeleteRange, Name = nameof(DeleteRangeItemAsync))]
    public async Task<IActionResult> DeleteRangeItemAsync([FromBody] RangeRequest<int> req)
    {
        return Ok(await _libraryItemService.DeleteRangeAsync(req.Ids));
    }

    [Authorize]
    [HttpPost(APIRoute.LibraryItem.Import, Name = nameof(ImportLibraryItemAsync))]
    public async Task<IActionResult> ImportLibraryItemAsync([FromForm] ImportBookEditionRequest req)
    {
       return Ok(await _libraryItemService.ImportAsync(
           file: req.File,
           coverImageFiles: req.CoverImageFiles,
           scanningFields: req.ScanningFields,
           duplicateHandle: req.DuplicateHandle));
    }
    
    [Authorize]
    [HttpGet(APIRoute.LibraryItem.Export, Name = nameof(ExportLibraryItemAsync))]
    public async Task<IActionResult> ExportLibraryItemAsync([FromQuery] LibraryItemSpecParams specParams)
    {
        var exportResult = await _libraryItemService.ExportAsync(new LibraryItemSpecification(
            specParams: specParams,
            pageIndex: specParams.PageIndex ?? 1,
            pageSize: specParams.PageSize ?? _appSettings.PageSize));
    
        return exportResult.Data is byte[] fileStream
            ? File(fileStream, @"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "library-items.xlsx")
            : Ok(exportResult);
    }
    
    [Authorize]
    [HttpPost(APIRoute.LibraryItem.CheckImagesForTraining, Name = nameof(CheckTrainingImagesForTraining))]
    public async Task<IActionResult> CheckTrainingImagesForTraining([FromForm] CheckImagesForTrainingRequest req)
    {
        return Ok(await _aiDetectionService.ValidateImportTraining(req.ItemId, req.CompareList));
    }
    #endregion

    [HttpGet(APIRoute.LibraryItem.Search, Name = nameof(SearchLibraryItemWithElasticAsync))]
    public async Task<IActionResult> SearchLibraryItemWithElasticAsync(
        [FromQuery] SearchItemRequest req, CancellationToken token)
    {
        return Ok(await _searchService.SearchItemAsync(req.ToSearchItemParams(), token));
    }

    [HttpGet(APIRoute.LibraryItem.GetRecentReadByIds, Name = nameof(GetLibraryItemByIdsAsync))]
    public async Task<IActionResult> GetLibraryItemByIdsAsync([FromQuery] RangeRequest<int> req, 
        [FromQuery] int? pageIndex, [FromQuery] int? pageSize)
    {
        return Ok(await _libraryItemService.GetRecentReadByIdsAsync(
            ids: req.Ids.ToArray(),
            pageIndex: pageIndex ?? 1,
            pageSize: pageSize ?? _appSettings.PageSize)
        );
    }

    [HttpGet(APIRoute.LibraryItem.GetTrending, Name = nameof(GetTrendingLibraryItemAsync))]
    public async Task<IActionResult> GetTrendingLibraryItemAsync([FromQuery] int? pageIndex, [FromQuery] int? pageSize)
    {
        return Ok(await _libraryItemService.GetTrendingAsync(
            pageIndex: pageIndex ?? 1,
            pageSize: pageSize ?? _appSettings.PageSize));
    }

    [HttpGet(APIRoute.LibraryItem.GetByCategory, Name = nameof(GetLibraryItemsByCategoryAsync))]
    public async Task<IActionResult> GetLibraryItemsByCategoryAsync([FromRoute] int categoryId,
        [FromQuery] int? pageIndex, [FromQuery] int? pageSize)
    {
        return Ok(await _libraryItemService.GetByCategoryAsync(
            categoryId: categoryId,
            pageIndex: pageIndex ?? 1,
            pageSize: pageSize ?? _appSettings.PageSize));
    }

    [HttpGet(APIRoute.LibraryItem.GetDetail, Name = nameof(GetLibraryItemDetailAsync))]
    public async Task<IActionResult> GetLibraryItemDetailAsync([FromRoute] int id)
    {
        return Ok(await _libraryItemService.GetDetailAsync(id));
    }

    [HttpGet(APIRoute.LibraryItem.GetDetailEditions, Name = nameof(GetLibraryItemEditionsAsync))]
    public async Task<IActionResult> GetLibraryItemEditionsAsync([FromRoute] int id,
        [FromQuery] int? pageIndex, [FromQuery] int? pageSize)
    {
        return Ok(await _libraryItemService.GetItemsInGroupAsync(
            id: id,
            pageIndex: pageIndex ?? 1,
            pageSize: pageSize ?? _appSettings.PageSize));
    }

    [HttpGet(APIRoute.LibraryItem.GetDetailReviews, Name = nameof(GetLibraryItemReviewsAsync))]
    public async Task<IActionResult> GetLibraryItemReviewsAsync([FromRoute] int id,
        [FromQuery] int? pageIndex, [FromQuery] int? pageSize)
    {
        return Ok(await _libraryItemService.GetReviewsAsync(
            id: id,
            pageIndex: pageIndex ?? 1,
            pageSize: pageSize ?? _appSettings.PageSize));
    }

    [HttpGet(APIRoute.LibraryItem.GetRelatedItems, Name = nameof(GetRelatedLibraryItemsAsync))]
    public async Task<IActionResult> GetRelatedLibraryItemsAsync([FromRoute] int id,
        [FromQuery] int? pageIndex, [FromQuery] int? pageSize)
    {
        return Ok(await _libraryItemService.GetRelatedItemsAsync(
            id: id,
            pageIndex: pageIndex ?? 1,
            pageSize: pageSize ?? _appSettings.PageSize));
    }
    
    [HttpGet(APIRoute.LibraryItem.GetRelatedAuthorItems, Name = nameof(GetAuthorRelatedLibraryItemsAsync))]
    public async Task<IActionResult> GetAuthorRelatedLibraryItemsAsync([FromQuery] int authorId,
        [FromQuery] int? pageIndex, [FromQuery] int? pageSize)
    {
        return Ok(await _authorService.GetRelatedAuthorItemsAsync(
            authorId: authorId,
            pageIndex: pageIndex ?? 1,
            pageSize: pageSize ?? _appSettings.PageSize));
    }
}