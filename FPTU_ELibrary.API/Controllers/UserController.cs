using FPTU_ELibrary.API.Extensions;
using FPTU_ELibrary.API.Payloads;
using FPTU_ELibrary.API.Payloads.Filters;
using FPTU_ELibrary.API.Payloads.Requests.Auth;
using FPTU_ELibrary.Application.Common;
using FPTU_ELibrary.Application.Dtos;
using FPTU_ELibrary.Application.Services.IServices;
using FPTU_ELibrary.Application.Validations;
using FPTU_ELibrary.Domain.Entities;
using FPTU_ELibrary.Domain.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace FPTU_ELibrary.API.Controllers;

[ApiController]
public class UserController:ControllerBase
{
    private readonly IUserService<UserDto> _userService;
    private readonly ISearchService _searchService;

    public UserController(IUserService<UserDto> userService,
        ISearchService searchService)
    {
        _userService = userService;
        _searchService = searchService;
    }

    [HttpGet(APIRoute.User.GetAll, Name = nameof(GetAllUserAsync))]
    [AllowAnonymous]
    public async Task<IActionResult> GetAllUserAsync()
    {
        var result = await _userService.GetAllAsync();
        if (result.Status == ResultConst.WARNING_NO_DATA_CODE) return NotFound("There is no user");
        return Ok(result);
    }
    [HttpGet(APIRoute.User.Search, Name = nameof(SearchUserAsync))]
    [AllowAnonymous]
    public async Task<IActionResult> SearchUserAsync([FromQuery] 
        string searchString)
    {
        var result = await _userService.SearchAccount(searchString);
        if (result.Status == ResultConst.WARNING_NO_DATA_CODE) return BadRequest("There is no user matched");
        return Ok(result);
    }

    [HttpPost(APIRoute.User.Create, Name = nameof(CreateUserAsync))]
    [AllowAnonymous]
    public async Task<IActionResult> CreateUserAsync([FromBody] CreateUserRequest req)
    {   
        return Ok(await _userService.CreateAccountByAdmin(req.ToUser()));
    }

    [HttpPut(APIRoute.User.ChangeAccountStatus,Name=nameof(ChangeAccountStatus))]
    [AllowAnonymous]
    public async Task<IActionResult> ChangeAccountStatus([FromRoute] Guid id)
    {  
        return Ok(await _userService.ChangeAccountStatus(id));
    }

    [HttpPatch(APIRoute.User.Update, Name = nameof(UpdateUserAsync))]
    [AllowAnonymous]
        public async Task<IActionResult> UpdateUserAsync([FromRoute] Guid id,[FromBody] UpdateUserRequest dto)
    {
        return Ok(await _userService.UpdateAccount(id, dto.ToUserForUpdate(),dto.ModifyBy));
    }

    [HttpPatch(APIRoute.User.UpdateRole,Name = nameof(UpdateRoleForGeneralUser))]
    [AllowAnonymous]
    public async Task<IActionResult> UpdateRoleForGeneralUser([FromRoute] Guid id,[FromBody] UpdateUserRequest dto)
    {
        return Ok(await _userService.UpdateAccount(id, dto.ToUpdateRoleUser(),dto.ModifyBy));
    }
    
    [HttpDelete(APIRoute.User.HardDelete,Name = nameof(DeleteUser))]
    [AllowAnonymous]
    public async Task<IActionResult> DeleteUser([FromRoute] Guid id)
    {
        return Ok(await _userService.DeleteAccount(id));
    }
    
    [HttpPost(APIRoute.User.CreateMany,Name = nameof(CreateManyAccountByAdmin))]
    [AllowAnonymous]
    public async Task<IActionResult> CreateManyAccountByAdmin([FromForm] CreateManyUsersRequest req)
    {
        return Ok(await _userService.CreateManyAccountsByAdmin(req.File));
    }
    
}