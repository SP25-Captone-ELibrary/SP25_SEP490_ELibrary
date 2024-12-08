using FPTU_ELibrary.Domain.Common.Enums;
using FPTU_ELibrary.Domain.Entities;
using FPTU_ELibrary.Domain.Interfaces.Services.Base;

namespace FPTU_ELibrary.Domain.Interfaces.Services;

public interface ISystemPermissionService<TDto> : IGenericService<SystemPermission, TDto, int>
    where TDto : class
{
    Task<IServiceResult> GetByPermissionNameAsync(Permission permission);
}