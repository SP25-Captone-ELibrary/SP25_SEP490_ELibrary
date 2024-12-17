using FPTU_ELibrary.API.Payloads;
using FPTU_ELibrary.Application.Configurations;
using FPTU_ELibrary.Application.Dtos.Books;
using FPTU_ELibrary.Domain.Interfaces.Services;
using FPTU_ELibrary.Domain.Specifications;
using FPTU_ELibrary.Domain.Specifications.Params;
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
    
    [HttpGet(APIRoute.Author.GetAll, Name = nameof(GetAllAuthorAsync))]
    public async Task<IActionResult> GetAllAuthorAsync([FromQuery] AuthorSpecParams specParams)
    {
        return Ok(await _authorService.GetAllWithSpecAsync(new AuthorSpecification(
            specParams: specParams, 
            pageIndex: specParams.PageIndex ?? 1,
            pageSize: specParams.PageSize ?? _appSettings.PageSize), tracked: false));
    }
}