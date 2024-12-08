using FPTU_ELibrary.Domain.Common.Enums;
using FPTU_ELibrary.Domain.Entities;
using FPTU_ELibrary.Domain.Interfaces.Services.Base;

namespace FPTU_ELibrary.Domain.Interfaces.Services;

public interface IRolePermissionService<TDto> : IGenericService<RolePermission, TDto, int> 
    where TDto : class
{
    Task<IServiceResult> CreateRoleWithDefaultPermissionsAsync(string engName, string viName, RoleType roleType);
    Task<IServiceResult> GetRolePermissionTableAsync(bool isRoleVerticalLayout);
    Task<IServiceResult> UpdatePermissionAsync(int colId, int rowId, int permissionId, bool isRoleVerticalLayout);
}