using System.Security.Claims;
using FPTU_ELibrary.API.Extensions;
using FPTU_ELibrary.API.Payloads;
using FPTU_ELibrary.API.Payloads.Filters;
using FPTU_ELibrary.API.Payloads.Requests.Auth;
using FPTU_ELibrary.Application.Common;
using FPTU_ELibrary.Application.Configurations;
using FPTU_ELibrary.Application.Dtos;
using FPTU_ELibrary.Application.Validations;
using FPTU_ELibrary.Domain.Common.Enums;
using FPTU_ELibrary.Domain.Entities;
using FPTU_ELibrary.Domain.Interfaces.Services;
using FPTU_ELibrary.Domain.Specifications;
using FPTU_ELibrary.Domain.Specifications.Params;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Nest;

namespace FPTU_ELibrary.API.Controllers;

[ApiController]
public class UserController:ControllerBase
{
    private readonly IUserService<UserDto> _userService;
    private readonly AppSettings _appSettings;

    public UserController(
        IOptionsMonitor<AppSettings> monitor,
        IUserService<UserDto> userService)
    {
        _userService = userService;
    }

     [Authorize]
     [HttpGet(APIRoute.User.GetAll, Name = nameof(GetAllUserAsync))]
     public async Task<IActionResult> GetAllUserAsync([FromQuery] UserSpecParams req)
     {
         return Ok(await _userService.GetAllWithSpecAsync(new UserSpecification(userSpecParams:req,
             pageIndex: req.PageIndex??1,pageSize: req.PageSize??5),tracked:false));
     }
    // [HttpGet(APIRoute.User.Search, Name = nameof(SearchUserAsync))]
    //[AllowAnonymous]
    //public async Task<IActionResult> SearchUserAsync([FromQuery] 
    //    string searchString)
    //{
    //    var result = await _userService.SearchAccount(searchString);
    //    if (result.Status == ResultConst.WARNING_NO_DATA_CODE) return BadRequest("There is no user matched");
    //    return Ok(result);
    //}
    
    [Authorize]
    [HttpGet(APIRoute.User.GetById, Name = nameof(GetUserByIdAsync))]
    public async Task<IActionResult> GetUserByIdAsync([FromRoute] Guid id)
    {
        return Ok(await _userService.GetByIdAsync(id));
    }

    [Authorize]
    [HttpPost(APIRoute.User.Create, Name = nameof(CreateUserAsync))]
    [AllowAnonymous]
    public async Task<IActionResult> CreateUserAsync([FromBody] CreateUserRequest req)
    {   
        return Ok(await _userService.CreateAccountByAdmin(req.ToUser()));
    }

    [Authorize]
    [HttpPut(APIRoute.User.ChangeAccountStatus,Name=nameof(ChangeAccountStatus))]
    public async Task<IActionResult> ChangeAccountStatus([FromRoute] Guid id)
    {  
        return Ok(await _userService.ChangeAccountStatus(id));
    }

    [Authorize]
    [HttpPatch(APIRoute.User.Update, Name = nameof(UpdateUserAsync))]
        public async Task<IActionResult> UpdateUserAsync([FromRoute] Guid id,[FromBody] UpdateUserRequest dto)
    {
        return Ok(await _userService.UpdateAccount(id, dto.ToUserForUpdate(),dto.ModifyBy));
    }

    [Authorize]
    [HttpDelete(APIRoute.User.HardDelete,Name = nameof(DeleteUser))]
    public async Task<IActionResult> DeleteUser([FromRoute] Guid id)
    {
        return Ok(await _userService.DeleteAccount(id));
    }
    
    // [HttpPost(APIRoute.User.CreateMany,Name = nameof(CreateManyAccountByAdmin))]
    // [AllowAnonymous]
    // public async Task<IActionResult> CreateManyAccountByAdmin([FromForm] CreateManyUsersRequest req)
    // {
    //     return Ok(await _userService.CreateManyAccountsByAdmin(req.File));
    // }
    [HttpPost(APIRoute.User.CreateManyWithSendEmail, Name = nameof(CreateManyAccountByAdminWithSendEmail))]
    [Authorize]
    public async Task<IActionResult> CreateManyAccountByAdminWithSendEmail([FromForm] CreateManyUsersRequest req)
    { 
        var email = User.FindFirst(ClaimTypes.Email)?.Value ?? "";
        return Ok(await _userService.CreateManyAccountsWithSendEmail(email,req.File, 
            Enum.Parse<DuplicateHandle>(req.DuplicateHandle)));
    }
}