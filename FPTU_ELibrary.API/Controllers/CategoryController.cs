using FPTU_ELibrary.API.Extensions;
using FPTU_ELibrary.API.Payloads;
using FPTU_ELibrary.API.Payloads.Requests;
using FPTU_ELibrary.Application.Configurations;
using FPTU_ELibrary.Application.Dtos.Books;
using FPTU_ELibrary.Application.Services.IServices;
using FPTU_ELibrary.Domain.Specifications;
using FPTU_ELibrary.Domain.Specifications.Params;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace FPTU_ELibrary.API.Controllers;

public class CategoryController : ControllerBase
{
    private readonly ICategoryService<CategoryDto> _categoryService;
    private readonly AppSettings _appSettings;

    public CategoryController(ICategoryService<CategoryDto> categoryService,
        IOptionsMonitor<AppSettings> appSettings)
    {
        _categoryService = categoryService;
        _appSettings = appSettings.CurrentValue;
    }

    [HttpPost(APIRoute.BookCategory.Create, Name = nameof(Create))]
    [Authorize]
    public async Task<IActionResult> Create([FromBody] CreateCategoryRequest req)
    {
        return Ok(await _categoryService.CreateAsync(req.ToCategoryDto()));
    }

    [HttpPatch(APIRoute.BookCategory.Update, Name = nameof(Update))]
    [Authorize]
    public async Task<IActionResult> Update([FromRoute] int id, [FromBody] UpdateCategoryRequest req)
    {
        return Ok(await _categoryService.UpdateAsync(id, req.ToCategoryForUpdate()));
    }

    [HttpDelete(APIRoute.BookCategory.HardDelete, Name = nameof(Delete))]
    [Authorize]
    public async Task<IActionResult> Delete([FromRoute] int id)
    {
        return Ok(await _categoryService.DeleteAsync(id));
    }


    [HttpGet(APIRoute.BookCategory.GetAll, Name = nameof(GetAll))]
    [Authorize]
    public async Task<IActionResult> GetAll([FromQuery] CategorySpecParams categorySpecParams)
    {
        return Ok(await _categoryService.GetAllWithSpecAsync(new CategorySpecification(
            categorySpecParams: categorySpecParams, pageIndex: categorySpecParams.PageIndex ?? 1,
            pageSize: categorySpecParams.PageSize ?? _appSettings.PageSize), false));
    }
    
    [HttpDelete(APIRoute.BookCategory.HardDeleteRange, Name = nameof(DeleteRangeAsync))]
    [Authorize]
    public async Task<IActionResult> DeleteRangeAsync([FromBody] DeleteRangeRequest<int> req)
    {
        return Ok(await _categoryService.HardDeleteRangeAsync(req.Ids));
    }
}