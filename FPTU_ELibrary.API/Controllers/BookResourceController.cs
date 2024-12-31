using System.Security.Claims;
using FPTU_ELibrary.API.Extensions;
using FPTU_ELibrary.API.Payloads;
using FPTU_ELibrary.API.Payloads.Requests;
using FPTU_ELibrary.API.Payloads.Requests.Book;
using FPTU_ELibrary.API.Payloads.Requests.BookEdition;
using FPTU_ELibrary.Application.Configurations;
using FPTU_ELibrary.Application.Dtos.Books;
using FPTU_ELibrary.Domain.Interfaces.Services;
using FPTU_ELibrary.Domain.Specifications;
using FPTU_ELibrary.Domain.Specifications.Params;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace FPTU_ELibrary.API.Controllers;

[ApiController]
public class BookResourceController : ControllerBase
{
    private readonly IBookResourceService<BookResourceDto> _bookResourceService;
    private readonly AppSettings _appSettings;

    public BookResourceController(
        IBookResourceService<BookResourceDto> bookResourceService,
        IOptionsMonitor<AppSettings> monitor)
    {
        _bookResourceService = bookResourceService;
        _appSettings = monitor.CurrentValue;
    }

    [Authorize]
    [HttpGet(APIRoute.BookResource.GetAll, Name = nameof(GetAllBookResourceAsync))]
    public async Task<IActionResult> GetAllBookResourceAsync([FromQuery] BookResourceSpecParams specParams)
    {
        return Ok(await _bookResourceService.GetAllWithSpecAsync(new BookResourceSpecification(
            specParams: specParams,
            pageIndex: specParams.PageIndex ?? 1,
            pageSize: specParams.PageSize ?? _appSettings.PageSize)));
    }

    [Authorize]
    [HttpGet(APIRoute.BookResource.GetById, Name = nameof(GetBookResourceByIdAsync))]
    public async Task<IActionResult> GetBookResourceByIdAsync([FromRoute] int id)
    {
        return Ok(await _bookResourceService.GetByIdAsync(id));
    }
    
    [Authorize]
    [HttpPost(APIRoute.BookResource.AddToBook, Name = nameof(AddBookResourceToBookAsync))]
    public async Task<IActionResult> AddBookResourceToBookAsync([FromRoute] int bookId, [FromBody] CreateBookResourceRequest req)
    {
        // Retrieve user email from token
        var email = User.FindFirst(ClaimTypes.Email)?.Value;
        return Ok(await _bookResourceService.AddBookResourceToBookAsync(bookId, req.ToBookResourceDto(), email ?? string.Empty));
    }
    
    [Authorize]
    [HttpPut(APIRoute.BookResource.Update, Name = nameof(UpdateBookResourceAsync))]
    public async Task<IActionResult> UpdateBookResourceAsync([FromRoute] int id, [FromBody] UpdateBookResourceRequest req)
    {
        // Retrieve user email from token
        var email = User.FindFirst(ClaimTypes.Email)?.Value;
        return Ok(await _bookResourceService.UpdateAsync(id, req.ToBookResourceDto(), email ?? string.Empty));
    }

    [Authorize]
    [HttpPatch(APIRoute.BookResource.SoftDelete, Name = nameof(SoftDeleteBookResourceAsync))]
    public async Task<IActionResult> SoftDeleteBookResourceAsync([FromRoute] int id)
    {
        return Ok(await _bookResourceService.SoftDeleteAsync(id));
    }
    
    [Authorize]
    [HttpPatch(APIRoute.BookResource.SoftDeleteRange, Name = nameof(SoftDeleteRangeBookResourceAsync))]
    public async Task<IActionResult> SoftDeleteRangeBookResourceAsync([FromBody] RangeRequest<int> req)
    {
        return Ok(await _bookResourceService.SoftDeleteRangeAsync(req.Ids));
    }

    [Authorize]
    [HttpPatch(APIRoute.BookResource.UndoDelete, Name = nameof(UndoSoftDeleteBookResourceAsync))]
    public async Task<IActionResult> UndoSoftDeleteBookResourceAsync([FromRoute] int id)
    {
        return Ok(await _bookResourceService.UndoDeleteAsync(id));
    }
    
    [Authorize]
    [HttpPatch(APIRoute.BookResource.UndoDeleteRange, Name = nameof(UndoSoftDeleteRangeBookResourceAsync))]
    public async Task<IActionResult> UndoSoftDeleteRangeBookResourceAsync([FromBody] RangeRequest<int> req)
    {
        return Ok(await _bookResourceService.UndoDeleteRangeAsync(req.Ids));
    }

    [Authorize]
    [HttpDelete(APIRoute.BookResource.Delete, Name = nameof(DeleteBookResourceAsync))]
    public async Task<IActionResult> DeleteBookResourceAsync([FromRoute] int id)
    {
        return Ok(await _bookResourceService.DeleteAsync(id));
    }
    
    [Authorize]
    [HttpDelete(APIRoute.BookResource.DeleteRange, Name = nameof(DeleteRangeBookResourceAsync))]
    public async Task<IActionResult> DeleteRangeBookResourceAsync([FromBody] RangeRequest<int> req)
    {
        return Ok(await _bookResourceService.DeleteRangeAsync(req.Ids));
    }
}