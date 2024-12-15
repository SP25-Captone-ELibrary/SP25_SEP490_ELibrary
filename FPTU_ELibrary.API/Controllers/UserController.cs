using System.Security.Claims;
using FPTU_ELibrary.API.Extensions;
using FPTU_ELibrary.API.Payloads;
using FPTU_ELibrary.API.Payloads.Filters;
using FPTU_ELibrary.API.Payloads.Requests.Auth;
using FPTU_ELibrary.Application.Common;
using FPTU_ELibrary.Application.Dtos;
using FPTU_ELibrary.Application.Services;
using FPTU_ELibrary.Application.Validations;
using FPTU_ELibrary.Domain.Common.Enums;
using FPTU_ELibrary.Domain.Entities;
using FPTU_ELibrary.Domain.Interfaces.Services;
using FPTU_ELibrary.Domain.Specifications;
using FPTU_ELibrary.Domain.Specifications.Params;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Nest;

namespace FPTU_ELibrary.API.Controllers;

[ApiController]
public class UserController:ControllerBase
{
    private readonly IUserService<UserDto> _userService;
    

    public UserController(IUserService<UserDto> userService)
    {
        _userService = userService;
    }

     [HttpGet(APIRoute.User.GetAll, Name = nameof(GetAllUserAsync))]
     [AllowAnonymous]
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
    [HttpGet(APIRoute.User.GetById, Name = nameof(GetById))]
    public async Task<IActionResult> GetById([FromRoute] Guid id)
    {
        return Ok(await _userService.GetById(id));
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