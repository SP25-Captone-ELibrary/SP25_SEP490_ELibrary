using FPTU_ELibrary.API.Extensions;
using FPTU_ELibrary.API.Payloads;
using FPTU_ELibrary.API.Payloads.Requests;
using FPTU_ELibrary.API.Payloads.Requests.LibraryClosureDay;
using FPTU_ELibrary.Application.Configurations;
using FPTU_ELibrary.Application.Dtos.LibraryItems;
using FPTU_ELibrary.Domain.Interfaces.Services;
using FPTU_ELibrary.Domain.Specifications;
using FPTU_ELibrary.Domain.Specifications.Params;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace FPTU_ELibrary.API.Controllers;

public class LibraryClosureDayController : ControllerBase
{
    private readonly AppSettings _appSettings;
    private readonly ILibraryClosureDayService<LibraryClosureDayDto> _libClosureDaySvc;

    public LibraryClosureDayController(
        ILibraryClosureDayService<LibraryClosureDayDto> libClosureDaySvc,
        IOptionsMonitor<AppSettings> monitor)
    {
        _appSettings = monitor.CurrentValue;
        _libClosureDaySvc = libClosureDaySvc;
    }
    
    #region Management
    [Authorize]
    [HttpPost(APIRoute.LibraryClosureDay.Create, Name = nameof(CreateLibraryClosureDayAsync))]
    public async Task<IActionResult> CreateLibraryClosureDayAsync([FromBody] CreateLibraryClosureDayRequest req)
    {
        return Ok(await _libClosureDaySvc.CreateAsync(req.ToLibraryClosureDayDto()));
    }

    [Authorize]
    [HttpGet(APIRoute.LibraryClosureDay.GetAll, Name = nameof(GetAllLibraryClosureDayAsync))]
    public async Task<IActionResult> GetAllLibraryClosureDayAsync([FromQuery] LibraryClosureDaySpecParams specParams)
    {
        return Ok(await _libClosureDaySvc.GetAllWithSpecAsync(new LibraryClosureDaySpecification(
            specParams: specParams,
            pageIndex: specParams.PageIndex ?? 1,
            pageSize: specParams.PageSize ?? _appSettings.PageSize)));
    }

    [Authorize]
    [HttpGet(APIRoute.LibraryClosureDay.GetById, Name = nameof(GetLibraryClosureDayByIdAsync))]
    public async Task<IActionResult> GetLibraryClosureDayByIdAsync([FromRoute] int id)
    {
        return Ok(await _libClosureDaySvc.GetByIdAsync(id));
    }

    [Authorize]
    [HttpPut(APIRoute.LibraryClosureDay.Update, Name = nameof(UpdateLibraryClosureDayAsync))]
    public async Task<IActionResult> UpdateLibraryClosureDayAsync(
        [FromRoute] int id,
        [FromBody] UpdateLibraryClosureDayRequest req)
    {
        return Ok(await _libClosureDaySvc.UpdateAsync(id: id, dto: req.ToLibraryClosureDayDto()));
    }

    [Authorize]
    [HttpDelete(APIRoute.LibraryClosureDay.Delete, Name = nameof(DeleteLibraryClosureDayAsync))]
    public async Task<IActionResult> DeleteLibraryClosureDayAsync([FromRoute] int id)
    {
        return Ok(await _libClosureDaySvc.DeleteAsync(id));
    }
    
    [Authorize]
    [HttpDelete(APIRoute.LibraryClosureDay.DeleteRange, Name = nameof(DeleteRangeLibraryClosureDayAsync))]
    public async Task<IActionResult> DeleteRangeLibraryClosureDayAsync([FromBody] RangeRequest<int> req)
    {
        return Ok(await _libClosureDaySvc.DeleteRangeAsync(req.Ids));
    }
    #endregion
    
    [HttpGet(APIRoute.LibraryClosureDay.GetAllPublic, Name = nameof(GetPublicAllLibraryClosureDayAsync))]
    public async Task<IActionResult> GetPublicAllLibraryClosureDayAsync([FromQuery] LibraryClosureDaySpecParams specParams)
    {
        return Ok(await _libClosureDaySvc.GetAllWithSpecAsync(new LibraryClosureDaySpecification(
            specParams: specParams,
            pageIndex: specParams.PageIndex ?? 1,
            pageSize: specParams.PageSize ?? _appSettings.PageSize)));
    }

    [HttpGet(APIRoute.LibraryClosureDay.GetByIdPublic, Name = nameof(GetPublicLibraryClosureDayByIdAsync))]
    public async Task<IActionResult> GetPublicLibraryClosureDayByIdAsync([FromRoute] int id)
    {
        return Ok(await _libClosureDaySvc.GetByIdAsync(id));
    }
}