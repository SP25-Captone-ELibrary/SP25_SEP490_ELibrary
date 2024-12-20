using System.Security.Claims;
using CsvHelper.Configuration.Attributes;
using FPTU_ELibrary.API.Extensions;
using FPTU_ELibrary.API.Payloads;
using FPTU_ELibrary.API.Payloads.Requests;
using FPTU_ELibrary.Application.Configurations;
using FPTU_ELibrary.Application.Dtos;
using FPTU_ELibrary.Application.Services.IServices;
using FPTU_ELibrary.Domain.Specifications;
using FPTU_ELibrary.Domain.Specifications.Params;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace FPTU_ELibrary.API.Controllers;

public class BookCategoryController : ControllerBase
{
    private readonly IBookCategoryService<BookCategoryDto> _bookCategoryService;
    private readonly AppSettings _appSettings;

    public BookCategoryController(IBookCategoryService<BookCategoryDto> bookCategoryService,
        IOptionsMonitor<AppSettings> appSettings)
    {
        _bookCategoryService = bookCategoryService;
        _appSettings = appSettings.CurrentValue;
    }

    [HttpPost(APIRoute.BookCategory.Create, Name = nameof(Create))]
    [Authorize]
    public async Task<IActionResult> Create([FromBody] CreateBookCategoryRequest req)
    {
        return Ok(await _bookCategoryService.CreateAsync(req.ToBookCategoryDto()));
    }

    [HttpPatch(APIRoute.BookCategory.Update, Name = nameof(Update))]
    [Authorize]
    public async Task<IActionResult> Update([FromRoute] int id, [FromBody] UpdateBookCategoryRequest req)
    {
        return Ok(await _bookCategoryService.UpdateAsync(id, req.ToBookCategoryForUpdate()));
    }

    [HttpDelete(APIRoute.BookCategory.HardDelete, Name = nameof(Delete))]
    [Authorize]
    public async Task<IActionResult> Delete([FromRoute] int id)
    {
        return Ok(await _bookCategoryService.DeleteAsync(id));
    }


    [HttpGet(APIRoute.BookCategory.GetAll, Name = nameof(GetAll))]
    [Authorize]
    public async Task<IActionResult> GetAll([FromQuery] BookCategorySpecParams bookCategorySpecParams)
    {
        bookCategorySpecParams.IsDelete = false;
        return Ok(await _bookCategoryService.GetAllWithSpecAsync(new BookCategorySpecification(
            bookCategorySpecParams: bookCategorySpecParams, pageIndex: bookCategorySpecParams.PageIndex ?? 1,
            pageSize: bookCategorySpecParams.PageSize ?? _appSettings.PageSize), false));
    }

    [HttpPatch(APIRoute.BookCategory.SoftDelete, Name = nameof(SoftDeleteAsync))]
    [Authorize]
    public async Task<IActionResult> SoftDeleteAsync([FromRoute] int id)
    {
        return Ok(await _bookCategoryService.SoftDeleteAsync(id));
    }

    [HttpPatch(APIRoute.BookCategory.SoftDeleteRange, Name = nameof(SoftDeleteRangeAsync))]
    [Authorize]
    public async Task<IActionResult> SoftDeleteRangeAsync([FromBody] DeleteRangeRequest<int> req)
    {
        return Ok(await _bookCategoryService.SoftDeleteRangeAsync(req.Ids));
    }

    [HttpPatch(APIRoute.BookCategory.UndoDelete, Name = nameof(UndoDeleteAsync))]
    [Authorize]
    public async Task<IActionResult> UndoDeleteAsync([FromRoute] int id)
    {
        return Ok(await _bookCategoryService.UndoDeleteAsync(id));
    }

    [HttpPatch(APIRoute.BookCategory.UndoDeleteRange, Name = nameof(UndoDeleteRangeAsync))]
    [Authorize]
    public async Task<IActionResult> UndoDeleteRangeAsync([FromBody] DeleteRangeRequest<int> req)
    {
        return Ok(await _bookCategoryService.UndoDeleteRangeAsync(req.Ids));
    }

    [HttpDelete(APIRoute.BookCategory.HardDeleteRange, Name = nameof(DeleteRangeAsync))]
    [Authorize]
    public async Task<IActionResult> DeleteRangeAsync([FromBody] DeleteRangeRequest<int> req)
    {
        return Ok(await _bookCategoryService.HardDeleteRangeAsync(req.Ids));
    }
}