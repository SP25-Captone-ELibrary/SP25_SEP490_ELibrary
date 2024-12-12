using FPTU_ELibrary.Domain.Common.Enums;
using FPTU_ELibrary.Domain.Entities;
using FPTU_ELibrary.Domain.Interfaces.Services.Base;
using FPTU_ELibrary.Domain.Specifications.Interfaces;
using Microsoft.AspNetCore.Http;

namespace FPTU_ELibrary.Domain.Interfaces.Services
{
	public interface IEmployeeService<TDto> : IGenericService<Employee, TDto, Guid>
		where TDto : class
	{
		Task<IServiceResult> GetByEmailAndPasswordAsync(string email, string password);
		Task<IServiceResult> GetByEmailAsync(string email);
		Task<IServiceResult> UpdateRoleAsync(int roleId, Guid employeeId);
		Task<IServiceResult> UpdateWithoutValidationAsync(Guid employeeId, TDto dto);
		Task<IServiceResult> UpdateEmailVerificationCodeAsync(Guid employeeId, string code);
		Task<IServiceResult> ChangeActiveStatusAsync(Guid employeeId);
		Task<IServiceResult> SoftDeleteAsync(Guid employeeId);
		Task<IServiceResult> ImportAsync(IFormFile? file, DuplicateHandle duplicateHandle, 
			string? columnSeparator, string? encodingType, string[]? scanningFields);
	}
}
