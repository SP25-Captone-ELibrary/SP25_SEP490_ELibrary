using FPTU_ELibrary.Application.Dtos;
using FPTU_ELibrary.Application.Utils;
using FPTU_ELibrary.Domain.Entities;
using FPTU_ELibrary.Domain.Interfaces.Services;
using FPTU_ELibrary.Domain.Interfaces;
using FPTU_ELibrary.Domain.Specifications;
using MapsterMapper;
using FPTU_ELibrary.Domain.Interfaces.Services.Base;
using FPTU_ELibrary.Application.Common;
using FPTU_ELibrary.Application.Dtos.Roles;
using FPTU_ELibrary.Application.Services.IServices;
using FPTU_ELibrary.Domain.Common.Enums;
using FPTU_ELibrary.Domain.Specifications.Interfaces;
using Mapster;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace FPTU_ELibrary.Application.Services
{
	public class EmployeeService : GenericService<Employee, EmployeeDto, Guid>, IEmployeeService<EmployeeDto>
	{
		private readonly ISystemRoleService<SystemRoleDto> _roleService;

		public EmployeeService(
			ISystemRoleService<SystemRoleDto> roleService,
			ISystemMessageService msgService,
			IUnitOfWork unitOfWork, 
			IMapper mapper,
			ILogger logger)
			: base(msgService, unitOfWork, mapper, logger)
		{
			_roleService = roleService;
		}

		public override async Task<IServiceResult> GetAllWithSpecAsync(
			ISpecification<Employee> specification,
			bool tracked = true)
		{
			try
			{
				// Try to parse specification to EmployeeSpecification
				var employeeSpec = specification as EmployeeSpecification;
				// Check if specification is null
				if (employeeSpec == null)
				{
					return new ServiceResult(ResultCodeConst.SYS_Fail0002,
						await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0002));
				}				
				
				// Define a local Mapster configuration
				var localConfig = new TypeAdapterConfig();
				localConfig.NewConfig<Employee, EmployeeDto>()
					.Ignore(dest => dest.PasswordHash!)
					.Ignore(dest => dest.RoleId)
					.Ignore(dest => dest.EmailConfirmed)
					.Ignore(dest => dest.TwoFactorEnabled)
					.Ignore(dest => dest.PhoneNumberConfirmed)
					.Ignore(dest => dest.TwoFactorSecretKey!)
					.Ignore(dest => dest.TwoFactorBackupCodes!)
					.Ignore(dest => dest.PhoneVerificationCode!)
					.Ignore(dest => dest.EmailVerificationCode!)
					.Ignore(dest => dest.PhoneVerificationExpiry!)
					.Map(dto => dto.Role, src => src.Role) 
					.AfterMapping((src, dest) => { dest.Role.RoleId = 0; });
				
				// Count total employees
				var totalEmployeeWithSpec = await _unitOfWork.Repository<Employee, Guid>().CountAsync(employeeSpec);
				// Count total page
				var totalPage = (int)Math.Ceiling((double)totalEmployeeWithSpec / employeeSpec.PageSize);
				
				// Set pagination to specification after count total employees 
				if (employeeSpec.PageIndex > totalPage 
					|| employeeSpec.PageIndex < 1) // Exceed total page or page index smaller than 1
				{
					employeeSpec.PageIndex = 1; // Set default to first page
				}
				
				// Apply pagination
				employeeSpec.ApplyPaging(
					skip: employeeSpec.PageSize * (employeeSpec.PageIndex - 1), 
					take: employeeSpec.PageSize);
				
				// Get all with spec
				var entities = await _unitOfWork.Repository<Employee, Guid>()
					.GetAllWithSpecAsync(employeeSpec, tracked);
				
				if (entities.Any()) // Exist data
				{
					// Convert to dto collection 
					var employeeDtos = entities.Adapt<IEnumerable<EmployeeDto>>(localConfig);
					
					// Pagination result 
					var paginationResultDto = new PaginatedResultDto<EmployeeDto>(employeeDtos,
						employeeSpec.PageIndex, employeeSpec.PageSize, totalPage);
					
					// Response with pagination 
					return new ServiceResult(ResultCodeConst.SYS_Success0002, 
						await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0002), paginationResultDto);
				}
				
				// Not found any data
				return new ServiceResult(ResultCodeConst.SYS_Warning0004, 
					await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0004),
					// Mapping entities to dto and ignore sensitive user data
					entities.Adapt<IEnumerable<EmployeeDto>>(localConfig));
			}
			catch (Exception ex)
			{
				_logger.Error(ex.Message);
				throw new Exception("Error invoke when progress get all data");
			}
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

		public async Task<IServiceResult> UpdateRoleAsync(int roleId, Guid employeeId)
		{
			try
			{
				// Get employee by id
				var employee = await _unitOfWork.Repository<Employee, Guid>().GetByIdAsync(employeeId);
				// Get role by id 
				var getRoleResult = await _roleService.GetByIdAsync(roleId);
				if (employee != null 
				    && getRoleResult.Data is SystemRoleDto role)
				{
					// Check is valid role type 
					if (role.RoleType != RoleType.Employee.ToString())
					{
						return new ServiceResult(ResultCodeConst.Role_Warning0002,
							await _msgService.GetMessageAsync(ResultCodeConst.Role_Warning0002));
					}
					
					// Progress update user role 
					employee.RoleId = role.RoleId;
					
					// Save to DB
					var isSaved = await _unitOfWork.SaveChangesAsync() > 0;
					if (isSaved) // Save success
					{
						return new ServiceResult(ResultCodeConst.SYS_Success0003,
							await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0003));
					}
					
					// Fail to update
					return new ServiceResult(ResultCodeConst.SYS_Fail0003,
						await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0003));
				}

				var errMSg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002); 
				return new ServiceResult(ResultCodeConst.SYS_Warning0002,
					StringUtils.Format(errMSg, "role or user"));
			}catch(Exception ex)
			{
				_logger.Error(ex.Message);
				throw new Exception("Error invoke when progress update user role");	
			}
		}
	}
}
