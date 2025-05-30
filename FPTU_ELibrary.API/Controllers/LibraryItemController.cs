using System.Security.Claims;
using FPTU_ELibrary.API.Extensions;
using FPTU_ELibrary.API.Payloads;
using FPTU_ELibrary.API.Payloads.Filters;
using FPTU_ELibrary.API.Payloads.Requests;
using FPTU_ELibrary.API.Payloads.Requests.Group;
using FPTU_ELibrary.API.Payloads.Requests.LibraryItem;
using FPTU_ELibrary.API.Payloads.Requests.OCR;
using FPTU_ELibrary.Application.Common;
using FPTU_ELibrary.Application.Configurations;
using FPTU_ELibrary.Application.Dtos.Authors;
using FPTU_ELibrary.Application.Dtos.LibraryItems;
using FPTU_ELibrary.Application.Dtos.Locations;
using FPTU_ELibrary.Application.Services.IServices;
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

    private readonly IAuthorService<AuthorDto> _authorService;
    private readonly ILibraryItemService<LibraryItemDto> _libraryItemService;
    private readonly ILibraryItemInstanceService<LibraryItemInstanceDto> _itemInstanceService;
    private readonly ILibraryItemAuthorService<LibraryItemAuthorDto> _itemAuthorService;
    private readonly IAIDetectionService _aiDetectionService;
    private readonly ILibraryShelfService<LibraryShelfDto> _shelfService;
    private readonly ISearchService _searchService;
    private readonly IS3Service _s3Service;
    private readonly ILibraryResourceService<LibraryResourceDto> _libraryResourceService;
    private readonly ILibraryItemGroupService<LibraryItemGroupDto> _libraryItemGroupService;

    public LibraryItemController(
        IAuthorService<AuthorDto> authorService,
        ILibraryItemService<LibraryItemDto> libraryItemService,
        ILibraryItemInstanceService<LibraryItemInstanceDto> itemInstanceService,
        ILibraryItemAuthorService<LibraryItemAuthorDto> itemAuthorService,
        ILibraryItemGroupService<LibraryItemGroupDto> libraryItemGroupService,
        ILibraryShelfService<LibraryShelfDto> shelfService,
        IAIDetectionService aiDetectionService,
        ISearchService searchService,
        IS3Service s3Service,
        ILibraryResourceService<LibraryResourceDto> libraryResourceService,
        IOptionsMonitor<AppSettings> monitor)
    {
        _authorService = authorService;
        _libraryItemService = libraryItemService;
        _itemInstanceService = itemInstanceService;
        _itemAuthorService = itemAuthorService;
        _aiDetectionService = aiDetectionService;
        _shelfService = shelfService;
        _searchService = searchService;
        _s3Service = s3Service;
        _libraryResourceService = libraryResourceService;
        _libraryItemGroupService = libraryItemGroupService;

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
        return Ok(await _libraryItemService.CreateAsync(
            dto: req.ToLibraryItemDto(), trackingDetailId: req.TrackingDetailId));
    }

    [Authorize]
    [HttpPost(APIRoute.LibraryItem.CreateGroup, Name = nameof(CreateLibraryItemGroupAsync))]
    public async Task<IActionResult> CreateLibraryItemGroupAsync([FromBody] CreateLibraryItemGroupRequest req)
    {
        var email = HttpContext.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
        return Ok(await _libraryItemGroupService.CreateAsync(
            dto: req.ToLibraryItemGroupDto(),
            createdByEmail: email ?? string.Empty));
    }

    [Authorize]
    [HttpPatch(APIRoute.LibraryItem.AssignToGroup, Name = nameof(AssignLibraryItemToGroupAsync))]
    public async Task<IActionResult> AssignLibraryItemToGroupAsync([FromRoute] int id, [FromRoute] int groupId)
    {
        return Ok(await _libraryItemService.AssignItemToGroupAsync(libraryItemId: id, groupId: groupId));
    }

    [Authorize]
    [HttpGet(APIRoute.LibraryItem.GetEnums, Name = nameof(GetLibraryItemEnumsAsync))]
    public async Task<IActionResult> GetLibraryItemEnumsAsync()
    {
        return Ok(await _libraryItemService.GetEnumValueAsync());
    }

    [Authorize]
    [HttpGet(APIRoute.LibraryItem.GetAll, Name = nameof(GetAllLibraryItemAsync))]
    public async Task<IActionResult> GetAllLibraryItemAsync([FromQuery] LibraryItemSpecParams specParams)
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
    [HttpGet(APIRoute.LibraryItem.GetGroupById, Name = nameof(GetGroupByLibraryItemIdAsync))]
    public async Task<IActionResult> GetGroupByLibraryItemIdAsync([FromRoute] int id,
        [FromQuery] int? pageIndex, [FromQuery] int? pageSize)
    {
        return Ok(await _libraryItemService.GetItemsInGroupAsync(
            id: id,
            pageIndex: pageIndex ?? 1,
            pageSize: pageSize ?? _appSettings.PageSize));
    }

    [Authorize]
    [HttpGet(APIRoute.LibraryItem.GetGroupableItems, Name = nameof(GetGroupableItemsAsync))]
    public async Task<IActionResult> GetGroupableItemsAsync(
        [FromQuery] LibraryItemGroupSpecParams specParams,
        [FromQuery] GetGroupableLibraryItemRequest req)
    {
        return Ok(await _libraryItemGroupService.GetAllPotentialGroupAsync(
            spec: new LibraryItemGroupSpecification(
                specParams: specParams,
                pageIndex: specParams.PageIndex ?? 1,
                pageSize: specParams.PageSize ?? _appSettings.PageSize),
            title: req.Title,
            cutterNumber: req.CutterNumber,
            classificationNumber: req.ClassificationNumber,
            authorName: req.AuthorName));
    }

    [Authorize]
    [HttpGet(APIRoute.LibraryItem.GetGroupableItemsById, Name = nameof(GetGroupableItemsByLibraryItemIdAsync))]
    public async Task<IActionResult> GetGroupableItemsByLibraryItemIdAsync(
        [FromQuery] LibraryItemGroupSpecParams specParams,
        [FromRoute] int id)
    {
        return Ok(await _libraryItemGroupService.GetAllPotentialGroupByLibraryItemIdAsync(
            spec: new LibraryItemGroupSpecification(
                specParams: specParams,
                pageIndex: specParams.PageIndex ?? 1,
                pageSize: specParams.PageSize ?? _appSettings.PageSize),
            libraryItemId: id));
    }

    [Authorize]
    [HttpGet(APIRoute.LibraryItem.GetShelf, Name = nameof(GetItemAppropriateShelfAsync))]
    public async Task<IActionResult> GetItemAppropriateShelfAsync([FromRoute] int id,
        [FromQuery] bool? isReferenceSection,
        [FromQuery] bool? isChildrenSection,
        [FromQuery] bool? isJournalSection,
        [FromQuery] bool? isMostAppropriate)
    {
        return Ok(await _shelfService.GetItemAppropriateShelfAsync(
            libraryItemId: id,
            isReferenceSection: isReferenceSection,
            isChildrenSection: isChildrenSection,
            isJournalSection: isJournalSection,
            isMostAppropriate: isMostAppropriate));
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
    [HttpGet(APIRoute.LibraryItem.Export, Name = nameof(ExportLibraryItemAsync))]
    public async Task<IActionResult> ExportLibraryItemAsync([FromQuery] LibraryItemSpecParams specParams)
    {
        var exportResult = await _libraryItemService.ExportAsync(new LibraryItemSpecification(
            specParams: specParams,
            pageIndex: specParams.PageIndex ?? 1,
            pageSize: specParams.PageSize ?? _appSettings.PageSize));

        return exportResult.Data is byte[] fileStream
            ? File(fileStream, @"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "library-items.xlsx")
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

    [HttpGet(APIRoute.LibraryItem.GetNewArrivals, Name = nameof(GetNewArrivalsLibraryItemAsync))]
    public async Task<IActionResult> GetNewArrivalsLibraryItemAsync([FromQuery] int? pageIndex,
        [FromQuery] int? pageSize)
    {
        return Ok(await _libraryItemService.GetNewArrivalsAsync(
            pageIndex: pageIndex ?? 1,
            pageSize: pageSize ?? _appSettings.PageSize));
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

    [HttpGet(APIRoute.LibraryItem.GetByBarcode, Name = nameof(GetLibraryItemByBarcodeAsync))]
    public async Task<IActionResult> GetLibraryItemByBarcodeAsync([FromQuery] string barcode)
    {
        return Ok(await _libraryItemService.GetByBarcodeAsync(barcode: barcode));
    }

    [HttpGet(APIRoute.LibraryItem.GetByIsbn, Name = nameof(GetLibraryItemByIsbnAsync))]
    public async Task<IActionResult> GetLibraryItemByIsbnAsync([FromQuery] string isbn)
    {
        return Ok(await _libraryItemService.GetByIsbnAsync(isbn: isbn));
    }

    [HttpGet(APIRoute.LibraryItem.GetDetail, Name = nameof(GetLibraryItemDetailAsync))]
    public async Task<IActionResult> GetLibraryItemDetailAsync([FromRoute] int id, [FromQuery] string? email = null)
    {
        return Ok(await _libraryItemService.GetDetailAsync(email: email, id: id));
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

    [HttpGet(APIRoute.LibraryItem.CheckUnavailableItems, Name = nameof(CheckUnavailableItemsAsync))]
    public async Task<IActionResult> CheckUnavailableItemsAsync([FromQuery] RangeRequest<int> req)
    {
        var email = HttpContext.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
        return Ok(await _libraryItemService.CheckUnavailableForBorrowRequestAsync(
            email: email ?? string.Empty,
            ids: req.Ids));
    }

    [Authorize]
    [HttpGet(APIRoute.LibraryItem.GetOwnResource, Name = nameof(GetFullPdfResourceAsync))]
    public async Task<IActionResult> GetFullPdfResourceAsync([FromRoute] int resourceId)
    {
        var email = User.FindFirst(ClaimTypes.Email)?.Value ?? "";

        var result = await _libraryResourceService.GetFullPdfFileWithWatermark(email, resourceId);
        if (result.ResultCode == ResultCodeConst.SYS_Success0002 && result.Data is (not null, not null))
        {
            //return base on the type of the resource
            if (result.Data.Item2.ToLower().Equals("image"))
            {
                return File(result.Data.Item1, "application/pdf", $"Watermarked_{resourceId}.pdf");
            }

            return File(result.Data.Item1, "audio/mpeg", "merged_audio.mp3");
        }

        return Ok(result);
    }

    [HttpGet(APIRoute.LibraryItem.GetPdfPreview, Name = nameof(GetPdfPreview))]
    public async Task<IActionResult> GetPdfPreview([FromRoute] int resourceId)
    {
        var email = User.FindFirst(ClaimTypes.Email)?.Value ?? "";
        var result = await _libraryResourceService.GetPdfPreview(resourceId);

        if (result.ResultCode == ResultCodeConst.SYS_Success0002 && result.Data is not null)
        {
            return File(result.Data, "application/pdf", $"Watermarked_{resourceId}.pdf");
        }

        return Ok(result);
    }

    [Authorize]
    [HttpGet(APIRoute.LibraryItem.GetFullAudioFileWithWatermark, Name = nameof(GetFullAudioResourceAsync))]
    public async Task<IActionResult> GetFullAudioResourceAsync([FromRoute] int resourceId)
    {
        var email = HttpContext.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
        return Ok(await _libraryResourceService.GetFullAudioFileWithWatermark(
            email: email ?? string.Empty,
            resourceId: resourceId));
    }

    [HttpGet(APIRoute.LibraryItem.GetAudioPreview, Name = nameof(GetAudioPreview))]
    public async Task<IActionResult> GetAudioPreview([FromRoute] int resourceId)
    {
        var result = await _libraryResourceService.GetAudioPreviewFromAws(resourceId);
        if (result.ResultCode == ResultCodeConst.SYS_Success0002 && result.Data is not null)
        {
            return File(result.Data, "audio/mpeg", $"audio.mp3");
        }

        return Ok(result);
    }

    [Authorize]
    [HttpGet(APIRoute.LibraryItem.CountPartToUpload, Name = nameof(CountPartToUpload))]
    public async Task<IActionResult> CountPartToUpload([FromRoute] int resourceId)
    {
        var email = HttpContext.User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
        return Ok(await _libraryResourceService.GetNumberOfUploadAudioFile(
            email: email ?? string.Empty,
            resourceId: resourceId));
    }

    [Authorize]
    [HttpGet(APIRoute.LibraryItem.GetOriginalAudioFile, Name = nameof(GetOriginalAudioFile))]
    public async Task<IActionResult> GetOriginalAudioFile([FromRoute] int resourceId)
    {
        return Ok( await _libraryResourceService.GetFullOriginalAudio(resourceId));
    }
}