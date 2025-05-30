﻿using FPTU_ELibrary.Domain.Common.Enums;
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
		Task<IServiceResult> UpdateProfileAsync(string email, TDto employee);
		Task<IServiceResult> UpdateRoleAsync(Guid employeeId, int roleId);
		Task<IServiceResult> UpdateWithoutValidationAsync(Guid employeeId, TDto dto);
		Task<IServiceResult> UpdateEmailVerificationCodeAsync(Guid employeeId, string code);
		Task<IServiceResult> ChangeActiveStatusAsync(Guid employeeId);
		Task<IServiceResult> SoftDeleteAsync(Guid employeeId);
		Task<IServiceResult> SoftDeleteRangeAsync(Guid[] employeeIds);
		Task<IServiceResult> UndoDeleteAsync(Guid employeeId);
		Task<IServiceResult> UndoDeleteRangeAsync(Guid[] employeeIds);
		Task<IServiceResult> DeleteRangeAsync(Guid[] employeeIds);
		Task<IServiceResult> ImportAsync(IFormFile? file, DuplicateHandle duplicateHandle, 
			string? columnSeparator, string? encodingType, string[]? scanningFields);
		Task<IServiceResult> ExportAsync(ISpecification<Employee> spec);
		Task<IServiceResult> UpdateMfaSecretAndBackupAsync(string email, string mfaKey, IEnumerable<string> backupCodes);
		Task<IServiceResult> UpdateMfaStatusAsync(Guid employeeId);
	}
}
