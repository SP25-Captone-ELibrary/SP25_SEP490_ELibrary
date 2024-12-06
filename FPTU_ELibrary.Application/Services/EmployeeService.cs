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
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace FPTU_ELibrary.Application.Services
{
	public class EmployeeService : GenericService<Employee, EmployeeDto, Guid>, IEmployeeService<EmployeeDto>
	{
		public EmployeeService(
			ISystemMessageService msgService,
			IUnitOfWork unitOfWork, 
			IMapper mapper,
			ILogger logger)
			: base(msgService, unitOfWork, mapper, logger)
		{
		}

		public async Task<IServiceResult> UpdateWithoutValidationAsync(Guid employeeId, EmployeeDto dto)
		{
			// Initiate service result
			var serviceResult = new ServiceResult();

			try
			{
				// Retrieve the entity
				var existingEntity = await _unitOfWork.Repository<Employee, Guid>().GetByIdAsync(employeeId);
				if (existingEntity == null)
				{
					return new ServiceResult(ResultCodeConst.SYS_Fail0002, 
						await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0002));
				}

				// Process add update entity
				// Map properties from dto to existingEntity
				_mapper.Map(dto, existingEntity);

				// Check if there are any differences between the original and the updated entity
				if (!_unitOfWork.Repository<Employee, Guid>().HasChanges(existingEntity))
				{
					serviceResult.ResultCode = ResultCodeConst.SYS_Success0003;
					serviceResult.Message = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0003);
					serviceResult.Data = true;
					return serviceResult;
				}

				// Progress update when all require passed
				await _unitOfWork.Repository<Employee, Guid>().UpdateAsync(existingEntity);

				// Save changes to DB
				var rowsAffected = await _unitOfWork.SaveChangesAsync();
				if (rowsAffected == 0)
				{
					serviceResult.ResultCode = ResultCodeConst.SYS_Fail0003;
					serviceResult.Message = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0003);
					serviceResult.Data = false;
					return serviceResult;
				}

				// Mark as update success
				serviceResult.ResultCode = ResultCodeConst.SYS_Success0003;
				serviceResult.Message = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0003);
				serviceResult.Data = true;
			}
			catch (Exception ex)
			{
				_logger.Error(ex.Message);
				throw new Exception("Error invoke while update employee");
			}

			return serviceResult;
		}
		
		public async Task<IServiceResult> GetByEmailAndPasswordAsync(string email, string password)
		{
			// Query specification
			var baseSpec = new BaseSpecification<Employee>(u => u.Email.Equals(email));
			// Include job role
			baseSpec.ApplyInclude(q => 
				q.Include(e => e.Role));

			// Get user by query specification
			var employee = await _unitOfWork.Repository<Employee, Guid>().GetWithSpecAsync(baseSpec);

			// Verify whether the given password match password hash or not
			if (employee == null || !HashUtils.VerifyPassword(password, employee.PasswordHash!))
				return new ServiceResult(ResultCodeConst.SYS_Warning0004, 
					await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0004));

			return new ServiceResult(ResultCodeConst.SYS_Success0002, 
				await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0002),
				_mapper.Map<EmployeeDto?>(employee));
		}

		public async Task<IServiceResult> GetByEmailAsync(string email)
		{
			// Query specification
			var baseSpec = new BaseSpecification<Employee>(u => u.Email.Equals(email));
			// Include job role
			baseSpec.ApplyInclude(q => 
				q.Include(e => e.Role));

			// Get user by query specification
			var employee = await _unitOfWork.Repository<Employee, Guid>().GetWithSpecAsync(baseSpec);

			// Not exist employee
			if (employee == null)
				return new ServiceResult(ResultCodeConst.SYS_Warning0004, 
					await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0004));

			return new ServiceResult(ResultCodeConst.SYS_Success0002, 
				await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0002),
				_mapper.Map<EmployeeDto?>(employee));
		}
	}
}
