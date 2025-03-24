using System.Security.Claims;
using CloudinaryDotNet;
using FPTU_ELibrary.API.Extensions;
using FPTU_ELibrary.API.Payloads;
using FPTU_ELibrary.API.Payloads.Requests;
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
public class LibraryResourceController : ControllerBase
{
    private readonly ILibraryResourceService<LibraryResourceDto> _libraryResourceService;
    private readonly AppSettings _appSettings;
    
    public LibraryResourceController(
        ILibraryResourceService<LibraryResourceDto> libraryResourceService,
        IOptionsMonitor<AppSettings> monitor)
    {
        _libraryResourceService = libraryResourceService;
        _appSettings = monitor.CurrentValue;
    }

    #region Management
    [Authorize]
    [HttpGet(APIRoute.LibraryItemResource.GetAll, Name = nameof(GetAllBookResourceAsync))]
    public async Task<IActionResult> GetAllBookResourceAsync([FromQuery] LibraryResourceSpecParams specParams)
    {
        return Ok(await _libraryResourceService.GetAllWithSpecAsync(new LibraryResourceSpecification(
            specParams: specParams,
            pageIndex: specParams.PageIndex ?? 1,
            pageSize: specParams.PageSize ?? _appSettings.PageSize)));
    }
    
    [Authorize]
    [HttpGet(APIRoute.LibraryItemResource.GetById, Name = nameof(GetBookResourceByIdAsync))]
    public async Task<IActionResult> GetBookResourceByIdAsync([FromRoute] int id)
    {
        return Ok(await _libraryResourceService.GetByIdAsync(id));
    }
    
    [Authorize]
    [HttpPost(APIRoute.LibraryItemResource.AddToBook, Name = nameof(AddResourceToLibraryItemAsync))]
    public async Task<IActionResult> AddResourceToLibraryItemAsync([FromRoute] int libraryItemId, [FromBody] CreateLibraryResourceRequest req)
    {
        return Ok(await _libraryResourceService.AddResourceToLibraryItemAsync(libraryItemId, req.ToLibraryResourceDto()));
    }
    
    [Authorize]
    [HttpPut(APIRoute.LibraryItemResource.Update, Name = nameof(UpdateBookResourceAsync))]
    public async Task<IActionResult> UpdateBookResourceAsync([FromRoute] int id, [FromBody] UpdateLibraryResourceRequest req)
    {
        return Ok(await _libraryResourceService.UpdateAsync(id, req.ToLibraryResourceDto()));
    }
    
    
    [Authorize]
    [HttpPatch(APIRoute.LibraryItemResource.SoftDelete, Name = nameof(SoftDeleteLibraryResourceAsync))]
    public async Task<IActionResult> SoftDeleteLibraryResourceAsync([FromRoute] int id)
    {
        return Ok(await _libraryResourceService.SoftDeleteAsync(id));
    }
    
    [Authorize]
    [HttpPatch(APIRoute.LibraryItemResource.SoftDeleteRange, Name = nameof(SoftDeleteRangeLibraryResourceAsync))]
    public async Task<IActionResult> SoftDeleteRangeLibraryResourceAsync([FromBody] RangeRequest<int> req)
    {
        return Ok(await _libraryResourceService.SoftDeleteRangeAsync(req.Ids));
    }
    
    [Authorize]
    [HttpPatch(APIRoute.LibraryItemResource.UndoDelete, Name = nameof(UndoSoftDeleteLibraryResourceAsync))]
    public async Task<IActionResult> UndoSoftDeleteLibraryResourceAsync([FromRoute] int id)
    {
        return Ok(await _libraryResourceService.UndoDeleteAsync(id));
    }
    
    [Authorize]
    [HttpPatch(APIRoute.LibraryItemResource.UndoDeleteRange, Name = nameof(UndoSoftDeleteRangeLibraryResourceAsync))]
    public async Task<IActionResult> UndoSoftDeleteRangeLibraryResourceAsync([FromBody] RangeRequest<int> req)
    {
        return Ok(await _libraryResourceService.UndoDeleteRangeAsync(req.Ids));
    }
    
    [Authorize]
    [HttpDelete(APIRoute.LibraryItemResource.Delete, Name = nameof(DeleteLibraryResourceAsync))]
    public async Task<IActionResult> DeleteLibraryResourceAsync([FromRoute] int id)
    {
        return Ok(await _libraryResourceService.DeleteAsync(id));
    }
    
    [Authorize]
    [HttpDelete(APIRoute.LibraryItemResource.DeleteRange, Name = nameof(DeleteRangeLibraryResourceAsync))]
    public async Task<IActionResult> DeleteRangeLibraryResourceAsync([FromBody] RangeRequest<int> req)
    {
        return Ok(await _libraryResourceService.DeleteRangeAsync(req.Ids));
    }

    [Authorize]
    [HttpPost(APIRoute.LibraryItemResource.AddAudioBook, Name = nameof(AddAudioBook))]
    public async Task<IActionResult> AddAudioBook([FromRoute] int libraryItemId,[FromBody] CreateLibraryResourceWithLargeFileRequest req)
    {
        return Ok(await _libraryResourceService.AddResourceToLibraryItemAsync(libraryItemId,req.ToLibraryResourceDto(),
            req.ToChunkDetail()));
    }
    #endregion
    
    [Authorize]
    [HttpGet(APIRoute.LibraryItemResource.GetByIdPublic, Name = nameof(GetBookResourceByIdFromPublicAsync))]
    public async Task<IActionResult> GetBookResourceByIdFromPublicAsync([FromRoute] int id)
    {
        return Ok(await _libraryResourceService.GetByIdAsync(id));
    }
}