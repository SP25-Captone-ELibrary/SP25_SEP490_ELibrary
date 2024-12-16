using System.Security.Claims;
using CsvHelper.Configuration.Attributes;
using FPTU_ELibrary.API.Extensions;
using FPTU_ELibrary.API.Payloads;
using FPTU_ELibrary.API.Payloads.Requests;
using FPTU_ELibrary.Application.Dtos;
using FPTU_ELibrary.Application.Services.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FPTU_ELibrary.API.Controllers;

public class BookCategoryController : ControllerBase
{
    private readonly IBookCategoryService<BookCategoryDto> _bookCategoryService;
    public BookCategoryController(IBookCategoryService<BookCategoryDto> bookCategoryService)
    {
        _bookCategoryService = bookCategoryService;
    }

    [HttpPost(APIRoute.BookCategory.Create, Name = nameof(Create))]
    [Authorize]
    // [AllowAnonymous]
    public async Task<IActionResult> Create([FromBody] CreateBookCategoryRequest req)
    {
        var roleName = User.FindFirst(ClaimTypes.Role)?.Value ?? "";
        return Ok(await _bookCategoryService.CreateAsync(req.ToBookCategoryDto()));
    }
    [HttpPatch(APIRoute.BookCategory.Update, Name = nameof(Update))]
    // [Authorize]
    [AllowAnonymous]
    public async Task<IActionResult> Update([FromRoute] int id, [FromBody] UpdateBookCategoryRequest req)
    {
        var roleName = User.FindFirst(ClaimTypes.Role)?.Value ?? "";
        return Ok(await _bookCategoryService.UpdateAsync(id,req.ToBookCategoryForUpdate()));
    }
    
    //Hard delete
    [HttpDelete(APIRoute.BookCategory.Delete, Name = nameof(Delete))]
    // [Authorize]
    [AllowAnonymous]
    public async Task<IActionResult> Delete([FromRoute] int id)
    {
        var roleName = User.FindFirst(ClaimTypes.Role)?.Value ?? "";
        return Ok(await _bookCategoryService.DeleteAsync(id));
    }
    
    
    
}