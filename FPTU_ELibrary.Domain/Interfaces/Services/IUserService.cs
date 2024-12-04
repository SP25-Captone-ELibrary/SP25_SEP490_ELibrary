using FPTU_ELibrary.Domain.Entities;
using FPTU_ELibrary.Domain.Interfaces.Services.Base;

namespace FPTU_ELibrary.Domain.Interfaces.Services
{
    public interface IUserService<TDto> : IGenericService<User, TDto, Guid>
        where TDto : class
    {
        Task<IServiceResult> UpdateWithoutValidationAsync(Guid userId, TDto dto);
        Task<IServiceResult> GetByEmailAndPasswordAsync(string email, string password);
        Task<IServiceResult> GetByEmailAsync(string email);
    }
}
