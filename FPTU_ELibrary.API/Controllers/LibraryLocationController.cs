using FPTU_ELibrary.API.Extensions;
using FPTU_ELibrary.API.Payloads;
using FPTU_ELibrary.API.Payloads.Requests.LibraryItem;
using FPTU_ELibrary.Application.Configurations;
using FPTU_ELibrary.Application.Dtos.Locations;
using FPTU_ELibrary.Domain.Interfaces.Services;
using FPTU_ELibrary.Domain.Specifications;
using FPTU_ELibrary.Domain.Specifications.Params;
using iTextSharp.text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace FPTU_ELibrary.API.Controllers;

[ApiController]
public class LibraryLocationController : ControllerBase
{
    private readonly AppSettings _appSettings;
    
    private readonly ILibraryShelfService<LibraryShelfDto> _shelfService;
    private readonly ILibraryFloorService<LibraryFloorDto> _floorService;
    private readonly ILibrarySectionService<LibrarySectionDto> _sectionService;
    private readonly ILibraryZoneService<LibraryZoneDto> _zoneService;

    public LibraryLocationController(
        ILibraryFloorService<LibraryFloorDto> floorService,
        ILibraryZoneService<LibraryZoneDto> zoneService,
        ILibrarySectionService<LibrarySectionDto> sectionService,
        ILibraryShelfService<LibraryShelfDto> shelfService,
        IOptionsMonitor<AppSettings> monitor)
    {
        _appSettings = monitor.CurrentValue;
        _floorService = floorService;
        _shelfService = shelfService;
        _sectionService = sectionService;
        _zoneService = zoneService;
    }

    #region Management
    [HttpGet(APIRoute.LibraryLocation.GetAllShelves, Name = nameof(GetAllShelvesAsync))]
    public async Task<IActionResult> GetAllShelvesAsync([FromQuery] LibraryShelfSpecParams specParams)
    {
        return Ok(await _shelfService.GetAllWithSpecAsync(new LibraryShelfSpecification(
            specParams: specParams,
            pageIndex: specParams.PageIndex ?? 1,
            pageSize: specParams.PageSize ?? _appSettings.PageSize)));
    }
    
    [HttpGet(APIRoute.LibraryLocation.GetMapShelfDetailById, Name = nameof(GetMapShelfDetailByIdAsync))]
    public async Task<IActionResult> GetMapShelfDetailByIdAsync([FromRoute] int shelfId,
        [FromQuery] LibraryItemSpecParams specParams)
    {
        return Ok(await _shelfService.GetDetailAsync(
            shelfId: shelfId,
            spec: new LibraryItemSpecification(
                specParams: specParams,
                pageIndex: specParams.PageIndex ?? 1,
                pageSize: specParams.PageSize ?? _appSettings.PageSize)));
    }

    [Authorize]
    [HttpGet(APIRoute.LibraryLocation.GetShelfById, Name = nameof(GetShelfByIdAsync))]
    public async Task<IActionResult> GetShelfByIdAsync([FromRoute] int shelfId)
    {
        return Ok(await _shelfService.GetByIdAsync(shelfId));
    }
    
    [Authorize]
    [HttpPut(APIRoute.LibraryLocation.UpdateShelf, Name = nameof(UpdateShelfAsync))]
    public async Task<IActionResult> UpdateShelfAsync([FromRoute] int shelfId, [FromBody] UpdateShelfRequest req)
    {
        return Ok(await _shelfService.UpdateAsync(shelfId, req.ToLibraryShelfDto()));
    }
    #endregion
    
    #region Map
    [HttpGet(APIRoute.LibraryLocation.GetMapByFloorId, Name = nameof(GetMapByFloorIdAsync))]
    public async Task<IActionResult> GetMapByFloorIdAsync([FromRoute] int floorId)
    {
        return Ok(await _floorService.GetMapByFloorIdAsync(floorId: floorId));
    }
    #endregion
    
    [HttpGet(APIRoute.LibraryLocation.GetFloors, Name = nameof(GetAllLibraryFloorAsync))]
    public async Task<IActionResult> GetAllLibraryFloorAsync()
    {
        return Ok(await _floorService.GetAllAsync());
    }
    
    [HttpGet(APIRoute.LibraryLocation.GetShelvesForFilter, Name = nameof(GetAllLibraryShelvesAsync))]
    public async Task<IActionResult> GetAllLibraryShelvesAsync()
    {
        return Ok(await _shelfService.GetAllAsync());
    }

    [HttpGet(APIRoute.LibraryLocation.GetShelfWithFloorZoneSectionById, Name = nameof(GetShelfWithFloorZoneSectionByIdAsync))]
    public async Task<IActionResult> GetShelfWithFloorZoneSectionByIdAsync([FromRoute] int shelfId)
    {
        return Ok(await _shelfService.GetDetailWithFloorZoneSectionByIdAsync(shelfId: shelfId));
    }
    
    [HttpGet(APIRoute.LibraryLocation.GetZonesByFloorId, Name = nameof(GetAllLibraryZoneByFloorIdAsync))]
    public async Task<IActionResult> GetAllLibraryZoneByFloorIdAsync([FromQuery] int floorId)
    {
        return Ok(await _zoneService.GetAllByFloorIdAsync(floorId));
    }
    
    [HttpGet(APIRoute.LibraryLocation.GetSectionsByZoneId, Name = nameof(GetAllLibrarySectionByZoneIdAsync))]
    public async Task<IActionResult> GetAllLibrarySectionByZoneIdAsync([FromQuery] int zoneId)
    {
        return Ok(await _sectionService.GetAllByZoneIdAsync(zoneId));
    }
    
    [HttpGet(APIRoute.LibraryLocation.GetShelvesBySectionId, Name = nameof(GetAllLibraryShelfBySectionIdAsync))]
    public async Task<IActionResult> GetAllLibraryShelfBySectionIdAsync([FromQuery] int sectionId)
    {
        return Ok(await _shelfService.GetAllBySectionIdAsync(sectionId));
    }
}