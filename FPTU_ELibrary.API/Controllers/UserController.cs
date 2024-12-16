using FPTU_ELibrary.API.Extensions;
using FPTU_ELibrary.API.Payloads;
using FPTU_ELibrary.API.Payloads.Filters;
using FPTU_ELibrary.API.Payloads.Requests.Auth;
using FPTU_ELibrary.Application.Common;
using FPTU_ELibrary.Application.Configurations;
using FPTU_ELibrary.Application.Dtos;
using FPTU_ELibrary.Application.Extensions;
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
        _appSettings = monitor.CurrentValue;
    }

     [Authorize]
     [HttpGet(APIRoute.User.GetAll, Name = nameof(GetAllUserAsync))]
     public async Task<IActionResult> GetAllUserAsync([FromQuery] UserSpecParams req)
     {
         return Ok(await _userService.GetAllWithSpecAsync(new UserSpecification(userSpecParams:req,
             pageIndex: req.PageIndex ?? 1, 
             pageSize: req.PageSize ?? _appSettings.PageSize), tracked:false));
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
    
    [Authorize]
    [HttpPost(APIRoute.User.CreateManyWithSendEmail, Name = nameof(CreateManyAccountByAdminWithSendEmail))]
    public IActionResult CreateManyAccountByAdminWithSendEmail([FromForm] CreateManyUsersRequest req)
    {
        // Bắt đầu tác vụ nền
        _ = _userService.CreateManyAccountsWithSendEmail(req.File);

        // Retrieve global language
        var langStr = LanguageContext.CurrentLanguage;
        var langEnum = EnumExtensions.GetValueFromDescription<SystemLanguage>(langStr);

        // Response
        return langEnum switch 
        {
            // as vi
            SystemLanguage.Vietnamese => Ok("Đang tiến hành tạo tài khoản và gửi email"),
            // default as eng
            _ => Ok("Account creation and email sending process has started")
        };
    }
}