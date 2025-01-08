using FPTU_ELibrary.API.Payloads;
using FPTU_ELibrary.Application.Dtos.Locations;
using FPTU_ELibrary.Domain.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FPTU_ELibrary.API.Controllers;

[ApiController]
public class LibraryLocationController : ControllerBase
{
    private readonly ILibraryShelfService<LibraryShelfDto> _shelfService;
    private readonly ILibraryFloorService<LibraryFloorDto> _floorService;
    private readonly ILibrarySectionService<LibrarySectionDto> _sectionService;
    private readonly ILibraryZoneService<LibraryZoneDto> _zoneService;

    public LibraryLocationController(
        ILibraryFloorService<LibraryFloorDto> floorService,
        ILibraryZoneService<LibraryZoneDto> zoneService,
        ILibrarySectionService<LibrarySectionDto> sectionService,
        ILibraryShelfService<LibraryShelfDto> shelfService)
    {
        _floorService = floorService;
        _shelfService = shelfService;
        _sectionService = sectionService;
        _zoneService = zoneService;
    }
    
    [Authorize]
    [HttpGet(APIRoute.LibraryLocation.GetFloors, Name = nameof(GetAllLibraryFloorAsync))]
    public async Task<IActionResult> GetAllLibraryFloorAsync()
    {
        return Ok(await _floorService.GetAllAsync());
    }
    
    [Authorize]
    [HttpGet(APIRoute.LibraryLocation.GetShelvesForFilter, Name = nameof(GetAllLibraryShelvesAsync))]
    public async Task<IActionResult> GetAllLibraryShelvesAsync()
    {
        return Ok(await _shelfService.GetAllAsync());
    }
    
    [Authorize]
    [HttpGet(APIRoute.LibraryLocation.GetZonesByFloorId, Name = nameof(GetAllLibraryZoneByFloorIdAsync))]
    public async Task<IActionResult> GetAllLibraryZoneByFloorIdAsync([FromQuery] int floorId)
    {
        return Ok(await _zoneService.GetAllByFloorIdAsync(floorId));
    }
    
    [Authorize]
    [HttpGet(APIRoute.LibraryLocation.GetSectionsByZoneId, Name = nameof(GetAllLibrarySectionByZoneIdAsync))]
    public async Task<IActionResult> GetAllLibrarySectionByZoneIdAsync([FromQuery] int zoneId)
    {
        return Ok(await _sectionService.GetAllByZoneIdAsync(zoneId));
    }
    
    [Authorize]
    [HttpGet(APIRoute.LibraryLocation.GetShelvesBySectionId, Name = nameof(GetAllLibraryShelfBySectionIdAsync))]
    public async Task<IActionResult> GetAllLibraryShelfBySectionIdAsync([FromQuery] int sectionId)
    {
        return Ok(await _shelfService.GetAllBySectionIdAsync(sectionId));
    }
}