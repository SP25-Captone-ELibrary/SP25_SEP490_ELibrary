using FPTU_ELibrary.Application.Dtos;
using FPTU_ELibrary.Application.Utils;
using FPTU_ELibrary.Domain.Entities;
using FPTU_ELibrary.Domain.Interfaces.Services;
using FPTU_ELibrary.Domain.Interfaces;
using FPTU_ELibrary.Domain.Specifications;
using MapsterMapper;
using FPTU_ELibrary.Domain.Interfaces.Services.Base;
using FPTU_ELibrary.Application.Common;
using FPTU_ELibrary.Application.Services.IServices;

namespace FPTU_ELibrary.Application.Services
{
	public class EmployeeService : GenericService<Employee, EmployeeDto, Guid>, IEmployeeService<EmployeeDto>
	{
		public EmployeeService(
			IUnitOfWork unitOfWork, 
			IMapper mapper,
			ICacheService cacheService)
			: base(unitOfWork, mapper, cacheService)
		{
		}

		public async Task<IServiceResult> GetByEmailAndPasswordAsync(string email, string password)
		{
			// Query specification
			var baseSpec = new BaseSpecification<Employee>(u => u.Email.Equals(email));
			// Include job role
			baseSpec.AddInclude(u => u.JobRole);

			// Get user by query specification
			var employee = await _unitOfWork.Repository<Employee, Guid>().GetWithSpecAsync(baseSpec);

			// Verify whether the given password match password hash or not
			if (employee == null || !HashUtils.VerifyPassword(password, employee.PasswordHash!))
				return new ServiceResult(ResultCodeConst.SYS_Warning0004, 
					await _cacheService.GetMessageAsync(ResultCodeConst.SYS_Warning0004));

			return new ServiceResult(ResultCodeConst.SYS_Success0002, 
				await _cacheService.GetMessageAsync(ResultCodeConst.SYS_Success0002),
				_mapper.Map<EmployeeDto?>(employee));
		}

		public async Task<IServiceResult> GetByEmailAsync(string email)
		{
			// Query specification
			var baseSpec = new BaseSpecification<Employee>(u => u.Email.Equals(email));
			// Include job role
			baseSpec.AddInclude(u => u.JobRole);

			// Get user by query specification
			var employee = await _unitOfWork.Repository<Employee, Guid>().GetWithSpecAsync(baseSpec);

			// Not exist employee
			if (employee == null)
				return new ServiceResult(ResultCodeConst.SYS_Warning0004, 
					await _cacheService.GetMessageAsync(ResultCodeConst.SYS_Warning0004));

			return new ServiceResult(ResultCodeConst.SYS_Success0002, 
				await _cacheService.GetMessageAsync(ResultCodeConst.SYS_Success0002),
				_mapper.Map<EmployeeDto?>(employee));
		}
	}
}
