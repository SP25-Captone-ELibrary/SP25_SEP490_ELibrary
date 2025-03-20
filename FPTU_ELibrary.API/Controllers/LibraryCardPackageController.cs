using FPTU_ELibrary.API.Extensions;
using FPTU_ELibrary.API.Payloads;
using FPTU_ELibrary.API.Payloads.Requests.LibraryCard;
using FPTU_ELibrary.Application.Configurations;
using FPTU_ELibrary.Application.Dtos.LibraryCard;
using FPTU_ELibrary.Domain.Interfaces.Services;
using FPTU_ELibrary.Domain.Specifications;
using FPTU_ELibrary.Domain.Specifications.Params;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace FPTU_ELibrary.API.Controllers;

[ApiController]
public class LibraryCardPackageController : ControllerBase
{
    private readonly AppSettings _appSettings;
    private readonly ILibraryCardPackageService<LibraryCardPackageDto> _packageSvc;

    public LibraryCardPackageController(
        ILibraryCardPackageService<LibraryCardPackageDto> packageSvc,
        IOptionsMonitor<AppSettings> monitor)
    {
        _packageSvc = packageSvc;
        _appSettings = monitor.CurrentValue;
    }
    
    #region Management

    [Authorize]
    [HttpGet(APIRoute.LibraryCardPackage.GetAll, Name = nameof(GetAllLibraryCardPackageAsync))]
    public async Task<IActionResult> GetAllLibraryCardPackageAsync([FromQuery] LibraryCardPackageSpecParams specParams)
    {
        return Ok(await _packageSvc.GetAllWithSpecAsync(new LibraryCardPackageSpecification(
            specParams: specParams,
            pageIndex: specParams.PageIndex ?? 1,
            pageSize: specParams.PageSize ?? _appSettings.PageSize)));
    }
    
    [Authorize]
    [HttpGet(APIRoute.LibraryCardPackage.GetById, Name = nameof(GetLibraryCardPackageByIdAsync))]
    public async Task<IActionResult> GetLibraryCardPackageByIdAsync([FromRoute] int id)
    {
        return Ok(await _packageSvc.GetByIdAsync(id));
    }
    
    [Authorize]
    [HttpPost(APIRoute.LibraryCardPackage.Create, Name = nameof(CreateLibraryCardPackageAsync))]
    public async Task<IActionResult> CreateLibraryCardPackageAsync([FromBody] CreateLibraryCardPackageRequest req)
    {
        return Ok(await _packageSvc.CreateAsync(req.ToLibraryCardPackageDto()));
    }
    
    [Authorize]
    [HttpPut(APIRoute.LibraryCardPackage.Update, Name = nameof(UpdateLibraryCardPackageAsync))]
    public async Task<IActionResult> UpdateLibraryCardPackageAsync([FromRoute] int id, [FromBody] UpdateLibraryCardPackageRequest req)
    {
        return Ok(await _packageSvc.UpdateAsync(id, req.ToLibraryCardPackageDto()));
    }
    
    [Authorize]
    [HttpDelete(APIRoute.LibraryCardPackage.Delete, Name = nameof(DeleteLibraryCardPackageAsync))]
    public async Task<IActionResult> DeleteLibraryCardPackageAsync([FromRoute] int id)
    {
        return Ok(await _packageSvc.DeleteAsync(id));
    }
    #endregion

    [HttpGet(APIRoute.LibraryCardPackage.GetAllPublic, Name = nameof(GetAllLibraryCardPackagePublicAsync))]
    public async Task<IActionResult> GetAllLibraryCardPackagePublicAsync()
    {
        return Ok(await _packageSvc.GetAllAsync());
    }
    
    [HttpGet(APIRoute.LibraryCardPackage.GetByIdPublic, Name = nameof(GetLibraryCardPackageByIdPublicAsync))]
    public async Task<IActionResult> GetLibraryCardPackageByIdPublicAsync([FromRoute] int id)
    {
        return Ok(await _packageSvc.GetByIdAsync(id: id));
    }
}