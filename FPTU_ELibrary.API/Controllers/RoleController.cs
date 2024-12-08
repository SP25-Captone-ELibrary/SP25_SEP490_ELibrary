using FPTU_ELibrary.API.Payloads;
using FPTU_ELibrary.API.Payloads.Requests.Role;
using FPTU_ELibrary.Application.Common;
using FPTU_ELibrary.Application.Dtos;
using FPTU_ELibrary.Application.Dtos.Roles;
using FPTU_ELibrary.Application.Extensions;
using FPTU_ELibrary.Domain.Common.Enums;
using FPTU_ELibrary.Domain.Interfaces.Services;
using FPTU_ELibrary.Domain.Specifications;
using FPTU_ELibrary.Domain.Specifications.Params;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FPTU_ELibrary.API.Controllers;

[ApiController]
public class RoleController : ControllerBase
{
    private readonly IRolePermissionService<RolePermissionDto> _rolePerService;
    private readonly ISystemPermissionService<SystemPermissionDto> _permissionService;
    private readonly ISystemRoleService<SystemRoleDto> _roleService;
    private readonly ISystemFeatureService<SystemFeatureDto> _featureService;
    private readonly IEmployeeService<EmployeeDto> _employeeService;
    private readonly IUserService<UserDto> _userService;

    public RoleController(
        IUserService<UserDto> userService,
        IEmployeeService<EmployeeDto> employeeService,
        ISystemFeatureService<SystemFeatureDto> featureService,
        ISystemRoleService<SystemRoleDto> roleService,
        ISystemPermissionService<SystemPermissionDto> permissionService,
        IRolePermissionService<RolePermissionDto> rolePerService)
    {
        _roleService = roleService;
        _rolePerService = rolePerService;
        _userService = userService;
        _employeeService = employeeService;
        _featureService = featureService;
        _permissionService = permissionService;
    }

    // [Authorize]
    [HttpPost(APIRoute.Role.GetAllRoleType, Name = nameof(GetAllRoleTypeAsync))]
    public async Task<IActionResult> GetAllRoleTypeAsync()
    {
        return await Task.FromResult(Ok(Enum.GetValues<RoleType>().Select(rt => new
        {
            RoleTypeIdx = rt,
            Name = rt.GetDescription()
        })));
    }

    // [Authorize]
    [HttpGet(APIRoute.Role.GetAllRole, Name = nameof(GetAllRoleAsync))]
    public async Task<IActionResult> GetAllRoleAsync()
    {
        return Ok(await _roleService.GetAllAsync());
    }
    
    // [Authorize]
    [HttpPost(APIRoute.Role.CreateRole, Name = nameof(CreateRoleAsync))]
    public async Task<IActionResult> CreateRoleAsync([FromBody] CreateRoleRequest req)
    {
        return Ok(await _rolePerService.CreateRoleWithDefaultPermissionsAsync(req.EnglishName, req.VietnameseName, req.RoleTypeIdx));
    }
    
    // [Authorize]
    [HttpDelete(APIRoute.Role.DeleteRole, Name = nameof(DeleteRoleAsync))]
    public async Task<IActionResult> DeleteRoleAsync(int roleId)
    {
        return Ok(await _roleService.DeleteAsync(roleId));
    }
    
    // [Authorize]
    [HttpPatch(APIRoute.Role.UpdateUserRole, Name = nameof(UpdateUserRoleAsync))]
    public async Task<IActionResult> UpdateUserRoleAsync([FromBody] UpdateUserRoleRequest req)
    {
        return Ok(await _userService.UpdateRoleAsync(req.RoleId, req.UserId));
    }
    
    // [Authorize]
    [HttpPatch(APIRoute.Role.UpdateEmployeeRole, Name = nameof(UpdateEmployeeRoleAsync))]
    public async Task<IActionResult> UpdateEmployeeRoleAsync([FromBody] UpdateEmployeeRoleRequest req)
    {
        return Ok(await _employeeService.UpdateRoleAsync(req.RoleId, req.EmployeeId));
    }
    
    // [Authorize]
    [HttpGet(APIRoute.Role.GetAllUserRole, Name = nameof(GetAllUserRoleAsync))]
    public async Task<IActionResult> GetAllUserRoleAsync()
    {
        return Ok(await _roleService.GetAllByRoleType(RoleType.User));
    }
    
    // [Authorize]
    [HttpGet(APIRoute.Role.GetAllEmployeeRole, Name = nameof(GetAllEmployeeRoleAsync))]
    public async Task<IActionResult> GetAllEmployeeRoleAsync()
    {
        return Ok(await _roleService.GetAllByRoleType(RoleType.Employee));
    }
    
    // [Authorize]
    [HttpGet(APIRoute.Role.GetAllFeature, Name = nameof(GetAllFeatureAsync))]
    public async Task<IActionResult> GetAllFeatureAsync()
    {
        return Ok(await _featureService.GetAllAsync());
    }
    
    // [Authorize]
    [HttpGet(APIRoute.Role.GetAllPermission, Name = nameof(GetAllPermissionAsync))]
    public async Task<IActionResult> GetAllPermissionAsync()
    {
        return Ok(await _permissionService.GetAllAsync());
    }
    
    // [Authorize]
    [HttpGet(APIRoute.Role.GetRolePermissionTable, Name = nameof(GetRolePermissionTableAsync))]
    public async Task<IActionResult> GetRolePermissionTableAsync(bool isRoleVerticalLayout)
    {
        return Ok(await _rolePerService.GetRolePermissionTableAsync(isRoleVerticalLayout));        
    }
    
    // [Authorize]
    [HttpPatch(APIRoute.Role.UpdateRolePermission, Name = nameof(UpdateRolePermissionTableAsync))]
    public async Task<IActionResult> UpdateRolePermissionTableAsync([FromBody] UpdateRolePermissionRequest req)
    {
        return Ok(await _rolePerService.UpdatePermissionAsync(
            req.ColId, req.RowId, req.PermissionId, req.IsRoleVerticalLayout));  
    }
}