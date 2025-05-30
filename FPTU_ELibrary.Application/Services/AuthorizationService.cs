using FPTU_ELibrary.Application.Exceptions;
using FPTU_ELibrary.Application.Extensions;
using FPTU_ELibrary.Domain.Common.Enums;
using FPTU_ELibrary.Domain.Entities;
using FPTU_ELibrary.Domain.Interfaces;
using FPTU_ELibrary.Domain.Interfaces.Services;
using FPTU_ELibrary.Domain.Specifications;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;
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
            var featureEnum = (SystemFeatureEnum?) EnumExtensions.GetValueFromDescription<SystemFeatureEnum>(featureDesc);
            if (featureEnum == null) return true;
            
            // Check exist feature
            var featureEntity = await _unitOfWork.Repository<SystemFeature, int>()
                .GetWithSpecAsync(new BaseSpecification<SystemFeature>(
                    sf => sf.EnglishName.Equals(featureEnum.ToString())));
            if (featureEntity == null) // Not found root features
            {
                // Continue to check whether is combined route -> Allow to access if not found
                return await ProcessCheckIsCombinedRouteAsync(role, (SystemFeatureEnum) featureEnum, httpMethod);
            };

            // Process check root features
            return await CheckPermissionAsync(role, featureEntity.EnglishName, httpMethod);
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new ForbiddenException("You do not have permission to access this resource.");
        }
    }

    private SystemFeatureEnum? GetCombinedRoute(SystemFeatureEnum requestFeature)
    {
        #region LibraryItemManagement 
        // Route: [LibraryItemManagement] -> Combined with 
        // [DashboardManagement]
        if (requestFeature.Equals(SystemFeatureEnum.DashboardManagement))
        {
            return SystemFeatureEnum.LibraryItemManagement;
        }
        // [AuthorManagement]
        if (requestFeature.Equals(SystemFeatureEnum.AuthorManagement))
        {
            return SystemFeatureEnum.LibraryItemManagement;
        } 
        // [CategoryManagement]
        if (requestFeature.Equals(SystemFeatureEnum.CategoryManagement))
        {
            return SystemFeatureEnum.LibraryItemManagement;
        }
        // [ResourceManagement]
        if (requestFeature.Equals(SystemFeatureEnum.ResourceManagement))
        {
            return SystemFeatureEnum.LibraryItemManagement;
        }
        // [LibraryLocationManagement]
        if (requestFeature.Equals(SystemFeatureEnum.LibraryLocationManagement))
        {
            return SystemFeatureEnum.LibraryItemManagement;
        }
        // [LibraryCardManagement]
        if (requestFeature.Equals(SystemFeatureEnum.LibraryCardManagement))
        {
            return SystemFeatureEnum.LibraryItemManagement;
        }
        // [LibraryCardHolderManagement]
        if (requestFeature.Equals(SystemFeatureEnum.LibraryCardHolderManagement))
        {
            return SystemFeatureEnum.LibraryItemManagement;
        }
        // [LibraryCardPackageManagement]
        if (requestFeature.Equals(SystemFeatureEnum.LibraryCardPackageManagement))
        {
            return SystemFeatureEnum.LibraryItemManagement;
        }
        // [LibraryItemConditionManagement]
        if (requestFeature.Equals(SystemFeatureEnum.LibraryItemConditionManagement))
        {
            return SystemFeatureEnum.LibraryItemManagement;
        }
        // [AuditTrailManagement]
        if (requestFeature.Equals(SystemFeatureEnum.AuditTrailManagement))
        {
            return SystemFeatureEnum.LibraryItemManagement;
        }
        #endregion

        #region BorrowManagement
        // Route: [BorrowManagement] -> Combine with [ReservationManagement] and [NotificationManagement]
        // Is [ReturnManagement]
        if (requestFeature.Equals(SystemFeatureEnum.ReservationManagement))
        {
            return SystemFeatureEnum.BorrowManagement;
        }
        // Is [NotificationManagement]
        if (requestFeature.Equals(SystemFeatureEnum.NotificationManagement))
        {
            return SystemFeatureEnum.BorrowManagement;
        }
        #endregion

        #region RoleManagement
        // Route: [RoleManagement] -> Combine with [RoleAuditTrailManagement]
        // [RoleAuditTrailManagement]
        if (requestFeature.Equals(SystemFeatureEnum.RoleAuditTrailManagement))
        {
            return SystemFeatureEnum.RoleManagement;
        }
        #endregion
        
        #region WarehouseTrackingManagement
        // Route: [WarehouseTrackingManagement] -> Combine with [SupplierManagement]
        if (requestFeature.Equals(SystemFeatureEnum.SupplierManagement))
        {
            return SystemFeatureEnum.WarehouseTrackingManagement;
        }
        #endregion
        
        #region SystemConfigurationManagement
        // Route: [SystemConfigurationManagement] -> Combine with [SystemMessageManagement] 
        if (requestFeature.Equals(SystemFeatureEnum.SystemMessageManagement))
        {
            return SystemFeatureEnum.SystemConfigurationManagement;
        } 
        // [SystemConfigurationManagement] -> Combine with [LibraryClosureDaysManagement]
        if (requestFeature.Equals(SystemFeatureEnum.LibraryClosureDaysManagement))
        {
            return SystemFeatureEnum.SystemConfigurationManagement;
        }
        #endregion

        return null;
    }

    private async Task<bool> ProcessCheckIsCombinedRouteAsync(string role, SystemFeatureEnum requestFeature, string httpMethod)
    {
        // Check request feature name is in combined route
        var featureOfCombinedRoute = GetCombinedRoute(requestFeature);
        // Null -> Route valid to access, not required to check permission
        if (featureOfCombinedRoute == null) return true; 
        
        // Initialize root feature 
        SystemFeatureEnum? rootFeature = null;
        // Is [RoleManagement]
        if (featureOfCombinedRoute == SystemFeatureEnum.RoleManagement) 
        {
            rootFeature = SystemFeatureEnum.RoleManagement;
        // Is [LibraryItemManagement]
        }else if (featureOfCombinedRoute == SystemFeatureEnum.LibraryItemManagement)
        {
            rootFeature = SystemFeatureEnum.LibraryItemManagement;
        }
        
        return await CheckPermissionAsync(role, rootFeature.ToString(), httpMethod);
    }

    private async Task<bool> CheckPermissionAsync(string role, string? featureName, string httpMethod)
    {
        // Not exist feature that required permission  -> Allow to access 
        if(string.IsNullOrEmpty(featureName)) return true;
        
        // Process check permission
        // Build base spec 
        var baseSpec = new BaseSpecification<RolePermission>(
            rp => rp.Role.EnglishName.Equals(role) && // with specific role
                  rp.Feature.EnglishName.Equals(featureName)); // with specific feature
        // Include permission
        baseSpec.ApplyInclude(q => 
            q.Include(rp => rp.Permission));
            
        // Get role permission with spec
        var rolePermissions = 
            await _unitOfWork.Repository<RolePermission, int>().GetAllWithSpecAsync(baseSpec); 

        // Check for permission validity
        return rolePermissions.Any(rp => IsPermissionValid(
            rp.Permission.PermissionLevel, // Current level of permission 
            httpMethod)); // Get required permission based on HttpMethod
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