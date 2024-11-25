using FPTU_ELibrary.Domain.Common.Enums;
using FPTU_ELibrary.Domain.Entities;
using FPTU_ELibrary.Domain.Interfaces.Services.Base;

namespace FPTU_ELibrary.Domain.Interfaces.Services
{
    public interface ISystemRoleService<TDto> : IGenericService<SystemRole, TDto, int>
        where TDto : class
    {
        Task<IServiceResult> GetByNameAsync(Role role);
    }
}
