using FPTU_ELibrary.API.Extensions;
using FPTU_ELibrary.API.Payloads;
using FPTU_ELibrary.API.Payloads.Requests;
using FPTU_ELibrary.API.Payloads.Requests.Category;
using FPTU_ELibrary.API.Payloads.Requests.Fine;
using FPTU_ELibrary.Application.Configurations;
using FPTU_ELibrary.Application.Dtos.LibraryItems;
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

    #region Management
    [HttpPost(APIRoute.Category.Create, Name = nameof(CreateCategoryAsync))]
    [Authorize]
    public async Task<IActionResult> CreateCategoryAsync([FromBody] CreateCategoryRequest req)
    {
        return Ok(await _categoryService.CreateAsync(req.ToCategoryDto()));
    }
    
    [HttpPut(APIRoute.Category.Update, Name = nameof(UpdateCategoryAsync))]
    [Authorize]
    public async Task<IActionResult> UpdateCategoryAsync([FromRoute] int id, [FromBody] UpdateCategoryRequest req)
    {
        return Ok(await _categoryService.UpdateAsync(id, req.ToCategoryForUpdate()));
    }
    
    [HttpDelete(APIRoute.Category.HardDelete, Name = nameof(DeleteCategoryAsync))]
    [Authorize]
    public async Task<IActionResult> DeleteCategoryAsync([FromRoute] int id)
    {
        return Ok(await _categoryService.DeleteAsync(id));
    }
    
    
    [HttpGet(APIRoute.Category.GetAll, Name = nameof(GetAllCategoryAsync))]
    [Authorize]
    public async Task<IActionResult> GetAllCategoryAsync([FromQuery] CategorySpecParams categorySpecParams)
    {
        return Ok(await _categoryService.GetAllWithSpecAsync(new CategorySpecification(
            categorySpecParams: categorySpecParams, pageIndex: categorySpecParams.PageIndex ?? 1,
            pageSize: categorySpecParams.PageSize ?? _appSettings.PageSize), false));
    }
    
    [HttpGet(APIRoute.Category.GetById, Name = nameof(GetCategoryByIdAsync))]
    [Authorize]
    public async Task<IActionResult> GetCategoryByIdAsync([FromRoute] int id)
    {
        return Ok(await _categoryService.GetByIdAsync(id));
    }
    
    [HttpDelete(APIRoute.Category.HardDeleteRange, Name = nameof(DeleteRangeCategoryAsync))]
    [Authorize]
    public async Task<IActionResult> DeleteRangeCategoryAsync([FromBody] RangeRequest<int> req)
    {
        return Ok(await _categoryService.DeleteRangeAsync(req.Ids));
    }
    
    [HttpPost(APIRoute.Category.Import, Name = nameof(ImportCategoryAsync))]
    [Authorize]
    public async Task<IActionResult> ImportCategoryAsync([FromForm] ImportBookCategoryRequest req)
    {
        return Ok(await _categoryService.ImportCategoryAsync(req.File, req.DuplicateHandle));
    }
    

    #endregion
    
    [HttpGet(APIRoute.Category.GetAllPublic, Name = nameof(GetAllCategoryFromPublicAsync))]
    public async Task<IActionResult> GetAllCategoryFromPublicAsync([FromQuery] CategorySpecParams categorySpecParams)
    {
        return Ok(await _categoryService.GetAllWithSpecAsync(new CategorySpecification(
            categorySpecParams: categorySpecParams, pageIndex: categorySpecParams.PageIndex ?? 1,
            pageSize: categorySpecParams.PageSize ?? _appSettings.PageSize), false));
    }
}