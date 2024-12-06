using FPTU_ELibrary.Application.Exceptions;
using FPTU_ELibrary.Application.Extensions;
using FPTU_ELibrary.Domain.Common.Enums;
using FPTU_ELibrary.Domain.Entities;
using FPTU_ELibrary.Domain.Interfaces;
using FPTU_ELibrary.Domain.Interfaces.Services;
using FPTU_ELibrary.Domain.Specifications;
using MapsterMapper;
using Serilog;
using SystemFeature = FPTU_ELibrary.Domain.Entities.SystemFeature;
using SystemFeatureEnum = FPTU_ELibrary.Domain.Common.Enums.SystemFeature;

namespace FPTU_ELibrary.Application.Services;

public class AuthorizationService : IAuthorizationService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger _logger;

    public AuthorizationService(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }
    
    public async Task<bool> IsAuthorizedAsync(string role, string featureDesc, string httpMethod)
    {
        try
        {
            // Get feature enum from desc
            var featureEnum = EnumExtensions.GetValueFromDescription<SystemFeatureEnum>(featureDesc);
            if (featureEnum == null!) return true;
            
            // Check exist feature
            var featureEntity = await _unitOfWork.Repository<SystemFeature, int>()
                .GetWithSpecAsync(new BaseSpecification<SystemFeature>(
                    sf => sf.EnglishName.Equals(featureEnum.ToString())));
            // Allow to access if not exist in authorized features
            if (featureEntity == null) return true;
            
            // Build base spec 
            var baseSpec = new BaseSpecification<RolePermission>(
                rp => rp.Role.EnglishName.Equals(role) && // with specific role
                      rp.Feature.EnglishName.Equals(featureEnum.ToString())); // with specific feature
            // Include permission
            baseSpec.AddInclude(rolePermission => rolePermission.Permission);
            
            // Get role permission with spec
            var rolePermissions = 
                await _unitOfWork.Repository<RolePermission, int>().GetAllWithSpecAsync(baseSpec); 

            // Check for permission validity
            return rolePermissions.Any(rp => IsPermissionValid(
                rp.Permission.PermissionLevel, // Current level of permission 
                httpMethod)); // Get required permission based on HttpMethod
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new ForbiddenException();
        }
    }
    
    private bool IsPermissionValid(int permissionLevel, string httpMethod)
    {
        // Map the HTTP method to the required permission level
        var requiredLevel = httpMethod switch
        {
            "POST" => (int) PermissionLevel.Create,
            "PUT" or "PATCH" => (int) PermissionLevel.Modify,
            "GET" => (int) PermissionLevel.View,
            "DELETE" => (int) PermissionLevel.FullAccess,
            _ => (int) PermissionLevel.AccessDenied
        };
        
        // Validate if the current permission level satisfies the required level
        return permissionLevel >= requiredLevel && permissionLevel != 0;
    }
}