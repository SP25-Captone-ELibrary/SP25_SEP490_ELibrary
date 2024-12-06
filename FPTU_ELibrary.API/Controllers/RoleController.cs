using FPTU_ELibrary.API.Payloads;
using FPTU_ELibrary.Application.Dtos;
using FPTU_ELibrary.Domain.Interfaces.Services;
using FPTU_ELibrary.Domain.Specifications;
using FPTU_ELibrary.Domain.Specifications.Params;
using Microsoft.AspNetCore.Mvc;

namespace FPTU_ELibrary.API.Controllers;

[ApiController]
public class RoleController : ControllerBase
{
    private readonly IRolePermissionService<RolePermissionDto> _rolePerService;

    public RoleController(IRolePermissionService<RolePermissionDto> rolePerService)
    {
        _rolePerService = rolePerService;
    }
    
    [HttpGet(APIRoute.Role.GetAll, Name = nameof(GetAllRoleAsync))]
    public async Task<IActionResult> GetAllRoleAsync([FromQuery] RoleSpecParams req)
    {
        return Ok(await _rolePerService.GetAllWithSpecAsync(new RoleSpecification(req)));
    }
}