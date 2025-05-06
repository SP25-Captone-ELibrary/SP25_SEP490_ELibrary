using FPTU_ELibrary.Application.Common;
using FPTU_ELibrary.Application.Dtos;
using FPTU_ELibrary.Application.Dtos.Auth;
using FPTU_ELibrary.Application.Dtos.Employees;
using FPTU_ELibrary.Application.Dtos.Roles;
using FPTU_ELibrary.Application.Exceptions;
using FPTU_ELibrary.Application.Extensions;
using FPTU_ELibrary.Application.Utils;
using FPTU_ELibrary.Domain.Common.Enums;
using FPTU_ELibrary.Domain.Entities;
using FPTU_ELibrary.Domain.Interfaces;
using FPTU_ELibrary.Domain.Interfaces.Services;
using FPTU_ELibrary.Domain.Interfaces.Services.Base;
using FPTU_ELibrary.Domain.Specifications;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using Serilog;
using SystemFeature = FPTU_ELibrary.Domain.Common.Enums.SystemFeature;

namespace FPTU_ELibrary.Application.Services;

public class RolePermissionService : GenericService<RolePermission, RolePermissionDto, int>,
    IRolePermissionService<RolePermissionDto>
{
    private readonly ISystemFeatureService<SystemFeatureDto> _featureService;
    private readonly ISystemRoleService<SystemRoleDto> _roleService;
    private readonly ISystemPermissionService<SystemPermissionDto> _permissionService;
    private readonly IEmployeeService<EmployeeDto> _employeeService;
    private readonly IUserService<UserDto> _userService;
    
    public RolePermissionService(
        ISystemPermissionService<SystemPermissionDto> permissionService,
        ISystemFeatureService<SystemFeatureDto> featureService,
        ISystemRoleService<SystemRoleDto> roleService,
        IUserService<UserDto> userService,
        IEmployeeService<EmployeeDto> employeeService,
        ISystemMessageService msgService,
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger logger) 
        : base(msgService, unitOfWork, mapper, logger)
    {
        _featureService = featureService;
        _permissionService = permissionService;
        _roleService = roleService;
        _userService = userService;
        _employeeService = employeeService;
    }

    public override async Task<IServiceResult> GetByIdAsync(int id)
    {
        try
        {
            // Determine current system lang 
            var lang = (SystemLanguage?) EnumExtensions.GetValueFromDescription<SystemLanguage>(
                LanguageContext.CurrentLanguage);
            var isEng = lang == SystemLanguage.English;
            
            // Build spec
            var baseSpec = new BaseSpecification<RolePermission>(rp => rp.RolePermissionId == id);
            // Apply include
            baseSpec.ApplyInclude(q => q
                .Include(rp => rp.Feature)
                .Include(rp => rp.Role)
                .Include(rp => rp.Permission)
            );
            // Retrieve with spec
            var existingEntity = await _unitOfWork.Repository<RolePermission, int>().GetWithSpecAsync(baseSpec);
            if (existingEntity == null)
            {
                // Not found {0}
                var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
                return new ServiceResult(ResultCodeConst.SYS_Warning0002, 
                    StringUtils.Format(errMsg, isEng ? "role permission" : "quyền truy cập"));
            }
            
            // Get data successfully
            return new ServiceResult(ResultCodeConst.SYS_Success0002,
                await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0002),
                _mapper.Map<RolePermissionDto>(existingEntity));
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when process get role permission by id");
        }
    }

    public async Task<IServiceResult> CreateRoleWithDefaultPermissionsAsync(string engName, string viName, RoleType roleType)
    {
        try
        {
            // Check role name exist
            var isRoleNameExist = await _unitOfWork.Repository<SystemRole,int>().AnyAsync(r => 
                r.EnglishName == engName || r.VietnameseName == viName);
            if (isRoleNameExist)
            {
                return new ServiceResult(ResultCodeConst.Role_Warning0001,
                    await _msgService.GetMessageAsync(ResultCodeConst.Role_Warning0001));
            }
                
            // Get access denied permission
            var accessDeniedPer = 
                (await _permissionService.GetByPermissionNameAsync(Permission.AccessDenied)).Data as SystemPermissionDto;
            // Get all system features
            var features = (await _featureService.GetAllAsync()).Data as List<SystemFeatureDto>;
            
            if(accessDeniedPer != null 
               && features != null && features.Any())
            {
                // Initialize role permission collection
                List<RolePermission> rolePermissions = new();

                // Initialize new role 
                var roleDto = new SystemRoleDto
                {
                    EnglishName = engName,
                    VietnameseName = viName,
                    RoleType = roleType.ToString()
                };
                
                // Progress create new role 
                var newRole = _mapper.Map<SystemRole>(roleDto);
                var roleRepository = _unitOfWork.Repository<SystemRole, int>();
                await roleRepository.AddAsync(newRole);
                await _unitOfWork.SaveChangesAsync(); // Save to get the role ID
                
                // Iterate each features and assign its new role
                foreach (var feat in features)
                {
                    rolePermissions.Add(new()
                    {
                        FeatureId = feat.FeatureId,
                        PermissionId = accessDeniedPer.PermissionId,
                        RoleId = newRole.RoleId
                    });
                }                
            
                // Add range
                await _unitOfWork.Repository<RolePermission, int>().AddRangeAsync(rolePermissions);
                // Save changes to DB
                var rowsAffected = await _unitOfWork.SaveChangesAsync();
                if (rowsAffected > 0)
                {
                    // Create success
                    return new ServiceResult(ResultCodeConst.SYS_Success0001, 
                        await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0001));
                }
            }
            
            // Create fail
            return new ServiceResult(ResultCodeConst.SYS_Fail0001, 
                await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0001));
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke while progress create new role");
        }
    }

    public async Task<IServiceResult> GetRolePermissionTableAsync(bool isRoleVerticalLayout)
    {
        try
        {
            // Get RoleManagement feature
            var systemFeatureDto = (await _featureService.GetByNameAsync(SystemFeature.RoleManagement)).Data as SystemFeatureDto;
            if (systemFeatureDto == null)
            {
                // Fail to get data
                return new ServiceResult(ResultCodeConst.SYS_Fail0002,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0002));
            }            
            
            // Base spec 
            var baseSpec = new BaseSpecification<RolePermission>();
            // Enable split query to improve query performance when include relation tables
            baseSpec.EnableSplitQuery();
            baseSpec.ApplyInclude(q => q
                // Include feature
                .Include(rp => rp.Feature)
                // Include permission
                .Include(rp => rp.Permission)
                // Include role
                .Include(rp => rp.Role)
            );

            // Get all role permissions
            var rolePermissions = (await _unitOfWork.Repository<RolePermission, int>()
                .GetAllWithSpecAsync(baseSpec)).ToList();

            if (rolePermissions.Any()) // Has any permissions
            {
                // Convert to Dto
                var rolePermissionDtos = _mapper.Map<List<RolePermissionDto>>(rolePermissions);

                // Get all system feature
                var getResult = isRoleVerticalLayout
                    ? await _featureService.GetAllAsync()
                    // Get all system roles for user-permission tables
                    : await _roleService.GetAllWithSpecAsync(new BaseSpecification<SystemRole>(
                        q => q.RoleType == nameof(RoleType.Employee) // With Employees
                             || q.EnglishName == nameof(Role.Administration))); // With Admin 
                
                if(getResult.ResultCode == ResultCodeConst.SYS_Success0002) // Retrieve data success
                {
                    // Try convert to role collection
                    var roles = getResult.Data as List<SystemRoleDto>;
                    // Try convert to feature collection
                    var features = getResult.Data as List<SystemFeatureDto>;
                    
                    // Progress generate user permission table
                    return new ServiceResult(ResultCodeConst.SYS_Success0002,
                        await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0002), 
                        rolePermissionDtos.ToUserPermissionTable(isRoleVerticalLayout, systemFeatureDto.FeatureId, roles, features));
                } 
            }
            
            // Response fail to generate user permission table
            return new ServiceResult(ResultCodeConst.SYS_Warning0004,
                await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0004), 
                new UserPermissionTableDto());
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when get role permission table");
        }
    }

    public async Task<IServiceResult> GetFeaturePermissionAsync(int featureId, string email, bool? isEmployee)
    {
        try
        {
            // Check whether user is User/Employee/Admin
            AuthenticateUserDto? authUser;

            if (isEmployee == null) // Missing token claims
            {
                // Mark as forbidden
                throw new ForbiddenException("Not allow to access");
            }

            // Check user type 
            if (isEmployee == true)
            {
                var employeeDto = (await _employeeService.GetByEmailAsync(email)).Data as EmployeeDto;
                // Map to authenticate user
                authUser = employeeDto?.ToAuthenticateUserDto();
            }
            else
            {
                var userDto = (await _userService.GetByEmailAsync(email)).Data as UserDto;
                // Map to authenticate user
                authUser = userDto?.ToAuthenticateUserDto();

                // Check if user is not admin
                if (userDto?.Role.EnglishName != nameof(Role.Administration))
                {
                    // Mark as forbidden
                    throw new ForbiddenException("Not allow to access");
                }
            }

            // Check exist authorized user
            if (authUser == null)
            {
                // Fail to get data
                return new ServiceResult(ResultCodeConst.SYS_Fail0002,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0002));
            }

            // Build base specification
            var baseSpec = new BaseSpecification<RolePermission>(
                rp => rp.RoleId == authUser.RoleId && rp.FeatureId == featureId);
            // Include permission
            baseSpec.ApplyInclude(q => q.Include(rp => rp.Permission));

            var rolePerEntity = await _unitOfWork.Repository<RolePermission, int>()
                .GetWithSpecAsync(baseSpec);
            if (rolePerEntity != null)
            {
                return new ServiceResult(ResultCodeConst.SYS_Success0002,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0002),
                    _mapper.Map<SystemPermissionDto>(rolePerEntity.Permission));
            }

            // Fail to get data
            return new ServiceResult(ResultCodeConst.SYS_Fail0002,
                await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0002));
        }
        catch (ForbiddenException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when get feature permission");
        }
    }
    
    public async Task<IServiceResult> GetAuthorizedUserFeatureAsync(string email, bool? isEmployee)
    {
        try
        {
            // Check whether user is User/Employee/Admin
            AuthenticateUserDto? authUser;

            if (isEmployee == null) // Missing token claims
            {
                // Mark as forbidden
                throw new ForbiddenException("Not allow to access");
            }
            
            // Check user type 
            if (isEmployee == true)
            {
                var employeeDto = (await _employeeService.GetByEmailAsync(email)).Data as EmployeeDto;
                // Map to authenticate user
                authUser = employeeDto?.ToAuthenticateUserDto();
            }
            else
            {
                var userDto = (await _userService.GetByEmailAsync(email)).Data as UserDto;
                // Map to authenticate user
                authUser = userDto?.ToAuthenticateUserDto();
                
                // Check if user is not admin
                if (userDto?.Role.EnglishName != nameof(Role.Administration))
                {
                    // Mark as forbidden
                    throw new ForbiddenException("Not allow to access");
                }
            }
            
            // Check exist authorized user
            if (authUser == null)
            {
                // Fail to get data
                return new ServiceResult(ResultCodeConst.SYS_Fail0002,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0002));
            }
            
            // Build base specification
            var baseSpec = new BaseSpecification<RolePermission>(
                rp => rp.RoleId == authUser.RoleId);
            // Include feature
            baseSpec.ApplyInclude(q => q.Include(rp => rp.Feature));
           
            // Get role permissions based on user's role
            var rolePermissions = await _unitOfWork.Repository<RolePermission, int>()
                .GetAllWithSpecAsync(baseSpec);
            var permissions = rolePermissions.ToList();
            if (!permissions.Any())
            {
                // Not found any
                return new ServiceResult(ResultCodeConst.SYS_Warning0004, 
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0004));
            }
            
            // Get access denied permission
            var accessDeniedPer = 
                    (await _permissionService.GetByPermissionNameAsync(Permission.AccessDenied)).Data as SystemPermissionDto;
            // Check not exist permission
            if (accessDeniedPer == null)
            {
                return new ServiceResult(ResultCodeConst.SYS_Fail0002,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0002));
            }
            
            // Extract features
            var featuresOfAuthorizedUser = permissions
                .Where(f => f.PermissionId != accessDeniedPer.PermissionId) // Not include access denied
                .Select(rp => rp.Feature); // Select feature only
            
            return new ServiceResult(ResultCodeConst.SYS_Success0002, 
                await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0002),
                _mapper.Map<List<SystemFeatureDto>>(featuresOfAuthorizedUser));
            
        }
        catch (ForbiddenException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when process get authorized user feature");
        }
    }
    
    public async Task<IServiceResult> UpdatePermissionAsync(
        int colId, int rowId, int permissionId, bool isRoleVerticalLayout)
    {
        // Initialize service result
        var serviceResult = new ServiceResult();

        try
        {
            // Get RoleManagement feature
            var systemFeatureDto =
                (await _featureService.GetByNameAsync(SystemFeature.RoleManagement)).Data as SystemFeatureDto;
            if (systemFeatureDto == null)
            {
                // Fail to get data
                return new ServiceResult(ResultCodeConst.SYS_Fail0002,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0002));
            }

            // Determine role or feature base on layout
            var roleId = isRoleVerticalLayout ? rowId : colId;
            var featureId = isRoleVerticalLayout ? colId : rowId;

            // Check is update RoleManagement feature
            if (featureId == systemFeatureDto.FeatureId)
            {
                // Mark as forbidden
                throw new ForbiddenException("Not allow to access");
            }

            // Base spec 
            var baseSpec = new BaseSpecification<RolePermission>(
                rp => rp.RoleId == roleId
                      && rp.FeatureId == featureId);
            var rolePermission = await _unitOfWork.Repository<RolePermission, int>()
                .GetWithSpecAsync(baseSpec);
            if (rolePermission == null)
            {
                var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
                return new ServiceResult(ResultCodeConst.SYS_Warning0002,
                    StringUtils.Format(errMsg, "role permission"));
            }

            // Update permission 
            rolePermission.PermissionId = permissionId;

            // Save changes to DB
            var rowsAffected = await _unitOfWork.SaveChangesAsync();
            if (rowsAffected == 0)
            {
                return new ServiceResult(ResultCodeConst.SYS_Fail0003,
                    await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0003));
            }

            // Mark as update success
            serviceResult.ResultCode = ResultCodeConst.SYS_Success0003;
            serviceResult.Message = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0003);
            serviceResult.Data = (await GetRolePermissionTableAsync(isRoleVerticalLayout)).Data;
        }
        catch (ForbiddenException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.Error(ex.Message);
            throw new Exception("Error invoke when update permission matrix");
        }
        
        return serviceResult;
    }
}