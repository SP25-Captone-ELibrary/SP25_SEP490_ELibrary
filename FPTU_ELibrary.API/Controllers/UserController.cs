using System.Security.Claims;
using FPTU_ELibrary.API.Extensions;
using FPTU_ELibrary.API.Payloads;
using FPTU_ELibrary.API.Payloads.Requests;
using FPTU_ELibrary.API.Payloads.Requests.Auth;
using FPTU_ELibrary.API.Payloads.Requests.User;
using FPTU_ELibrary.Application.Configurations;
using FPTU_ELibrary.Application.Dtos;
using FPTU_ELibrary.Domain.Common.Enums;
using FPTU_ELibrary.Domain.Interfaces.Services;
using FPTU_ELibrary.Domain.Specifications;
using FPTU_ELibrary.Domain.Specifications.Params;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

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
        return Ok(await _userService.GetAllWithSpecAsync(new UserSpecification(
             userSpecParams: req,
             pageIndex: req.PageIndex ?? 1,
             pageSize: req.PageSize ?? _appSettings.PageSize), tracked: false));
    }
     
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
        return Ok(await _userService.CreateAccountByAdminAsync(req.ToUser()));
    }
    
    [Authorize]
    [HttpPatch(APIRoute.User.ChangeAccountStatus,Name=nameof(ChangeAccountStatus))]
    public async Task<IActionResult> ChangeAccountStatus([FromRoute] Guid id)
    {  
        return Ok(await _userService.ChangeActiveStatusAsync(id));
    }

    [Authorize]
    [HttpPut(APIRoute.User.Update, Name = nameof(UpdateUserAsync))]
    public async Task<IActionResult> UpdateUserAsync([FromRoute] Guid id, [FromBody] UpdateUserRequest dto)
    {
        return Ok(await _userService.UpdateAsync(id, dto.ToUserForUpdate()));
    }

    [Authorize]
    [HttpDelete(APIRoute.User.SoftDelete, Name = nameof(SoftDeleteUserAsync))]
    public async Task<IActionResult> SoftDeleteUserAsync([FromRoute] Guid id)
    {
        return Ok(await _userService.SoftDeleteAsync(id));
    }
    
    [Authorize]
    [HttpDelete(APIRoute.User.SoftDeleteRange, Name = nameof(SoftDeleteRangeUserAsync))]
    public async Task<IActionResult> SoftDeleteRangeUserAsync([FromBody] RangeRequest<Guid> req)
    {
        return Ok(await _userService.SoftDeleteRangeAsync(req.Ids));
    }
    
    [Authorize]
    [HttpDelete(APIRoute.User.UndoDelete, Name = nameof(UndoDeleteUserAsync))]
    public async Task<IActionResult> UndoDeleteUserAsync([FromRoute] Guid id)
    {
        return Ok(await _userService.UndoDeleteAsync(id));
    }
    
    [Authorize]
    [HttpDelete(APIRoute.User.UndoDeleteRange, Name = nameof(UndoDeleteRangeUserAsync))]
    public async Task<IActionResult> UndoDeleteRangeUserAsync([FromBody] RangeRequest<Guid> req)
    {
        return Ok(await _userService.UndoDeleteRangeAsync(req.Ids));
    }
        
    [Authorize]
    [HttpDelete(APIRoute.User.HardDelete,Name = nameof(DeleteUserAsync))]
    public async Task<IActionResult> DeleteUserAsync([FromRoute] Guid id)
    {
        return Ok(await _userService.DeleteAsync(id));
    }
    
    [Authorize]
    [HttpDelete(APIRoute.User.HardDeleteRange, Name = nameof(DeleteRangeUserAsync))]
    public async Task<IActionResult> DeleteRangeUserAsync([FromBody] RangeRequest<Guid> req)
    {
        return Ok(await _userService.DeleteRangeAsync(req.Ids));
    }
    
    [Authorize]
    [HttpPost(APIRoute.User.Import, Name = nameof(ImportUserAsync))]
    public async Task<IActionResult> ImportUserAsync([FromForm] CreateManyUsersRequest req)
    { 
        var email = User.FindFirst(ClaimTypes.Email)?.Value ?? "";
        return Ok(await _userService.CreateManyAccountsWithSendEmail(
            email: email, excelFile: req.File, 
            duplicateHandle: req.DuplicateHandle, isSendEmail: req.IsSendEmail));
    }
    
    [Authorize]
    [HttpGet(APIRoute.User.Export, Name = nameof(ExportUserAsync))]
    public async Task<IActionResult> ExportUserAsync([FromQuery] UserSpecParams specParams)
    {
        var exportResult = await _userService.ExportAsync(new UserSpecification(
            userSpecParams: specParams,
            pageIndex: specParams.PageIndex ?? 1,
            pageSize: specParams.PageSize ?? _appSettings.PageSize));

        return exportResult.Data is byte[] fileStream
            ? File(fileStream, @"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Employees.xlsx")
            : Ok(exportResult);
    }
}