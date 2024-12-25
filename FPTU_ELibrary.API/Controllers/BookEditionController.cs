using FPTU_ELibrary.API.Payloads;
using FPTU_ELibrary.Application.Configurations;
using FPTU_ELibrary.Application.Dtos.BookEditions;
using FPTU_ELibrary.Application.Dtos.Books;
using FPTU_ELibrary.Domain.Interfaces.Services;
using FPTU_ELibrary.Domain.Specifications;
using FPTU_ELibrary.Domain.Specifications.Params;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace FPTU_ELibrary.API.Controllers;

[ApiController]
public class BookEditionController : ControllerBase
{
    private readonly AppSettings _appSettings;
    private readonly IBookEditionService<BookEditionDto> _bookEditionService;

    public BookEditionController(
        IBookEditionService<BookEditionDto> bookEditionService,
        IOptionsMonitor<AppSettings> monitor)
    {
        _bookEditionService = bookEditionService;
        _appSettings = monitor.CurrentValue;
    }
    
    [Authorize]
    [HttpGet(APIRoute.BookEdition.GetAllEdition, Name = nameof(GetAllBookEditionAsync))]
    public async Task<IActionResult> GetAllBookEditionAsync([FromQuery] BookEditionSpecParams specParams)
    {
        return Ok(await _bookEditionService.GetAllWithSpecAsync(new BookEditionSpecification(
            specParams: specParams,
            pageIndex: specParams.PageIndex ?? 1,
            pageSize: specParams.PageSize ?? _appSettings.PageSize), tracked: false));
    }
    
    [Authorize]
    [HttpGet(APIRoute.BookEdition.GetEditionById, Name = nameof(GetBookEditionByIdAsync))]
    public async Task<IActionResult> GetBookEditionByIdAsync([FromRoute] int id)
    {
        return Ok(await _bookEditionService.GetByIdAsync(id));
    }
}