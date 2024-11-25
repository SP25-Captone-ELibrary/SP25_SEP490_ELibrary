using FPTU_ELibrary.Domain.Entities;
using FPTU_ELibrary.Domain.Interfaces.Services.Base;

namespace FPTU_ELibrary.Domain.Interfaces.Services
{
	public interface IRefreshTokenService<TDto> : IGenericService<RefreshToken, TDto, int>
		where TDto : class
	{
		Task<IServiceResult> GetByUserIdAsync(Guid userId);
		Task<IServiceResult> GetByEmployeeIdAsync(Guid employeeId);
		Task<IServiceResult> GetByEmailAsync(string email);
	}
}
