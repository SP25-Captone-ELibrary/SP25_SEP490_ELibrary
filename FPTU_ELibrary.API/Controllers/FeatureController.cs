using System.Security.Claims;
using FPTU_ELibrary.API.Payloads;
using FPTU_ELibrary.Application.Dtos.Roles;
using FPTU_ELibrary.Domain.Common.Constants;
using FPTU_ELibrary.Domain.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FPTU_ELibrary.API.Controllers;

[ApiController]
public class FeatureController : ControllerBase
{
    private readonly IRolePermissionService<RolePermissionDto> _rolePerService;

    public FeatureController(
        IRolePermissionService<RolePermissionDto> rolePerService)
    {
        _rolePerService = rolePerService;
    }
    
    
    [Authorize]
    [HttpGet(APIRoute.Feature.GetAuthorizedUserFeatures, Name = nameof(GetAuthorizedUserFeaturesAsync))]
    public async Task<IActionResult> GetAuthorizedUserFeaturesAsync()
    {
        // Retrieve email claim
        var email = User.FindFirst(ClaimTypes.Email)?.Value;
        // Retrieve user type claim
        var userType = User.FindFirst(CustomClaimTypes.UserType)?.Value;
        return Ok(await _rolePerService.GetAuthorizedUserFeatureAsync(email ?? string.Empty, 
            userType?.Equals(ClaimValues.EMPLOYEE_CLAIMVALUE)));
    }

    [Authorize]
    [HttpGet(APIRoute.Feature.GetFeaturePermission, Name = nameof(GetFeaturePermissionAsync))]
    public async Task<IActionResult> GetFeaturePermissionAsync([FromRoute] int id)
    {
        // Retrieve email claim
        var email = User.FindFirst(ClaimTypes.Email)?.Value;
        // Retrieve user type claim
        var userType = User.FindFirst(CustomClaimTypes.UserType)?.Value;
        return Ok(await _rolePerService.GetFeaturePermissionAsync(id, email ?? string.Empty, 
            userType?.Equals(ClaimValues.EMPLOYEE_CLAIMVALUE)));
    }
}