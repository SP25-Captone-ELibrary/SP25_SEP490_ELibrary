using System.Security.Claims;
using FPTU_ELibrary.API.Payloads;
using FPTU_ELibrary.Application.Configurations;
using FPTU_ELibrary.Application.Dtos.LibraryItems;
using FPTU_ELibrary.Domain.Interfaces.Services;
using FPTU_ELibrary.Domain.Specifications;
using FPTU_ELibrary.Domain.Specifications.Params;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace FPTU_ELibrary.API.Controllers;

public class UserFavoriteController: ControllerBase
{
    private readonly IUserFavoriteService<UserFavoriteDto> _userFavoriteService;
    private readonly AppSettings _monitor;

    public UserFavoriteController(IUserFavoriteService<UserFavoriteDto>userFavoriteService,
        IOptionsMonitor<AppSettings> monitor)
    {
        _userFavoriteService = userFavoriteService;
        _monitor = monitor.CurrentValue;
    }
    
    [Authorize]
    [HttpPost(APIRoute.UserFavorite.AddFavorite, Name = nameof(AddFavorite))]
    public async Task<IActionResult> AddFavorite([FromRoute] int id)
    {
        var email = User.FindFirst(ClaimTypes.Email)?.Value ?? string.Empty;
        return Ok(await _userFavoriteService.AddFavoriteAsync(id, email));
    }
    
    [Authorize]
    [HttpDelete(APIRoute.UserFavorite.RemoveFavorite, Name = nameof(RemoveFavorite))]
    public async Task<IActionResult> RemoveFavorite([FromRoute] int id)
    {
        var email = User.FindFirst(ClaimTypes.Email)?.Value ?? string.Empty;
        return Ok(await _userFavoriteService.RemoveFavoriteAsync(id, email));
    }
    
    [Authorize]
    [HttpGet(APIRoute.UserFavorite.GetAll, Name = nameof(GetAllUserFavoriteAsync))]
    public async Task<IActionResult> GetAllUserFavoriteAsync([FromQuery] UserFavoriteSpecParams specParams)
    {
        var email = User.FindFirst(ClaimTypes.Email)?.Value ?? string.Empty;
        return Ok(await _userFavoriteService.GetAllWithSpecAsync(new UserFavoriteSpecification(
            specParams: specParams,
            pageIndex: specParams.PageIndex ?? 1,
            pageSize: specParams.PageSize ?? _monitor.PageSize,email), tracked: false));
    }
}