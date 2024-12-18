using FPTU_ELibrary.API.Extensions;
using FPTU_ELibrary.API.Payloads;
using FPTU_ELibrary.API.Payloads.Requests;
using FPTU_ELibrary.API.Payloads.Requests.Author;
using FPTU_ELibrary.Application.Configurations;
using FPTU_ELibrary.Application.Dtos.Authors;
using FPTU_ELibrary.Application.Dtos.Books;
using FPTU_ELibrary.Domain.Interfaces.Services;
using FPTU_ELibrary.Domain.Specifications;
using FPTU_ELibrary.Domain.Specifications.Params;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace FPTU_ELibrary.API.Controllers;

[ApiController]
public class AuthorController : ControllerBase
{
    private readonly IAuthorService<AuthorDto> _authorService;
    private readonly AppSettings _appSettings;

    public AuthorController(
        IAuthorService<AuthorDto> authorService,
        IOptionsMonitor<AppSettings> appSettings)
    {
        _authorService = authorService;
        _appSettings = appSettings.CurrentValue;
    }

    #region Management
    [Authorize]
    [HttpGet(APIRoute.Author.GetAll, Name = nameof(GetAllAuthorAsync))]
    public async Task<IActionResult> GetAllAuthorAsync([FromQuery] AuthorSpecParams specParams)
    {
        return Ok(await _authorService.GetAllWithSpecAsync(new AuthorSpecification(
            specParams: specParams, 
            pageIndex: specParams.PageIndex ?? 1,
            pageSize: specParams.PageSize ?? _appSettings.PageSize), tracked: false));
    }

    [Authorize]
    [HttpGet(APIRoute.Author.GetById, Name = nameof(GetAuthorByIdAsync))]
    public async Task<IActionResult> GetAuthorByIdAsync([FromRoute] int id)
    {
        return Ok(await _authorService.GetByIdAsync(id));        
    }
    
    [Authorize]
    [HttpPost(APIRoute.Author.Create, Name = nameof(CreateAuthorAsync))]
    public async Task<IActionResult> CreateAuthorAsync([FromBody] CreateAuthorRequest req)
    {
        return Ok(await _authorService.CreateAsync(req.ToAuthorDto()));
    }

    [Authorize]
    [HttpPut(APIRoute.Author.Update, Name = nameof(UpdateAuthorAsync))]
    public async Task<IActionResult> UpdateAuthorAsync([FromRoute] int id, [FromBody] UpdateAuthorRequest req)
    {
        return Ok(await _authorService.UpdateAsync(id, req.ToAuthorDto()));
    }
    
    [Authorize]
    [HttpPatch(APIRoute.Author.SoftDelete, Name = nameof(SoftDeleteAuthorAsync))]
    public async Task<IActionResult> SoftDeleteAuthorAsync([FromRoute] int id)
    {
        return Ok(await _authorService.SoftDeleteAsync(id));
    }
    
    [Authorize]
    [HttpPatch(APIRoute.Author.SoftDeleteRange, Name = nameof(SoftDeleteRangeAuthorAsync))]
    public async Task<IActionResult> SoftDeleteRangeAuthorAsync([FromBody] DeleteRangeRequest<int> req)
    {
        return Ok(await _authorService.SoftDeleteRangeAsync(req.Ids));
    }
    
    [Authorize]
    [HttpPatch(APIRoute.Author.UndoDelete, Name = nameof(UndoDeleteAuthorAsync))]
    public async Task<IActionResult> UndoDeleteAuthorAsync([FromRoute] int id)
    {
        return Ok(await _authorService.UndoDeleteAsync(id));
    }
    
    [Authorize]
    [HttpPatch(APIRoute.Author.UndoDeleteRange, Name = nameof(UndoDeleteRangeAuthorAsync))]
    public async Task<IActionResult> UndoDeleteRangeAuthorAsync([FromBody] DeleteRangeRequest<int> req)
    {
        return Ok(await _authorService.UndoDeleteRangeAsync(req.Ids));
    }
    
    [Authorize]
    [HttpDelete(APIRoute.Author.Delete, Name = nameof(DeleteAuthorAsync))]
    public async Task<IActionResult> DeleteAuthorAsync([FromRoute] int id)
    {
        return Ok(await _authorService.DeleteAsync(id));
    }
    
    [Authorize]
    [HttpDelete(APIRoute.Author.DeleteRange, Name = nameof(DeleteRangeAuthorAsync))]
    public async Task<IActionResult> DeleteRangeAuthorAsync([FromBody] DeleteRangeRequest<int> req)
    {
        return Ok(await _authorService.DeleteRangeAsync(req.Ids));
    }
    
    #endregion
    
    [HttpGet(APIRoute.Author.GetAuthorDetail, Name = nameof(GetAuthorDetailByIdAsync))]
    public async Task<IActionResult> GetAuthorDetailByIdAsync([FromRoute] int id)
    {
        return Ok(await _authorService.GetAuthorDetailByIdAsync(id));        
    }
}