using FPTU_ELibrary.Application.Common;
using FPTU_ELibrary.Application.Dtos;
using FPTU_ELibrary.Application.Dtos.Roles;
using FPTU_ELibrary.Application.Services.IServices;
using FPTU_ELibrary.Application.Utils;
using FPTU_ELibrary.Domain.Common.Enums;
using FPTU_ELibrary.Domain.Entities;
using FPTU_ELibrary.Domain.Interfaces;
using FPTU_ELibrary.Domain.Interfaces.Services;
using FPTU_ELibrary.Domain.Interfaces.Services.Base;
using FPTU_ELibrary.Domain.Specifications;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using Serilog;
using SystemRole = FPTU_ELibrary.Domain.Entities.SystemRole;
namespace FPTU_ELibrary.Application.Services
{
	public class SystemRoleService : GenericService<SystemRole, SystemRoleDto, int>,
		ISystemRoleService<SystemRoleDto>
	{
		public SystemRoleService(
			ISystemMessageService msgService,
			IUnitOfWork unitOfWork,
			IMapper mapper,
			ILogger logger)
			: base(msgService, unitOfWork, mapper, logger)
		{
		}

		//	Override delete procedure
		public override async Task<IServiceResult> DeleteAsync(int id)
		{
			try
			{
				// Get role by id 
				var roleEntity = await _unitOfWork.Repository<SystemRole, int>().GetByIdAsync(id);
				if (roleEntity == null)
				{
					var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0002);
					return new ServiceResult(ResultCodeConst.SYS_Warning0002,
						StringUtils.Format(errMsg, "role"));
				}

				// Check whether role exist any users or employees
				var isExistUser = await _unitOfWork.Repository<SystemRole, int>()
					.AnyAsync(x => x.Users.Any(u => u.RoleId == id));
				var isExistEmployee = await _unitOfWork.Repository<SystemRole, int>()
					.AnyAsync(x => x.Employees.Any(u => u.RoleId == id));

				if (isExistUser || isExistEmployee) // Role has been in used
				{
					var errMsg = await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0007);
					return new ServiceResult(ResultCodeConst.SYS_Warning0007,
						StringUtils.Format(errMsg, "role"));
				}

				// Get all role permission by role id
				var rolePermissions = await _unitOfWork.Repository<RolePermission, int>()
					.GetAllWithSpecAsync(new BaseSpecification<RolePermission>(
						rp => rp.RoleId == id));

				// Progress delete user permissions
				foreach (var rp in rolePermissions)
				{
					await _unitOfWork.Repository<RolePermission, int>().DeleteAsync(rp.RolePermissionId);
				}

				// Progress delete role 
				await _unitOfWork.Repository<SystemRole, int>().DeleteAsync(id);

				// Save to DB with transaction
				if (await _unitOfWork.SaveChangesWithTransactionAsync() > 0)
				{
					// Update success
					return new ServiceResult(ResultCodeConst.SYS_Success0004,
						await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0004), true);
				}

				// Fail to update
				return new ServiceResult(ResultCodeConst.SYS_Fail0004,
					await _msgService.GetMessageAsync(ResultCodeConst.SYS_Fail0004), false);
			}
			catch (Exception ex)
			{
				_logger.Error(ex.Message);
				throw new Exception("Error invoke when progress delete role");
			}
		}

		public async Task<IServiceResult> GetByNameAsync(Role role)
		{
			try
			{
				// Initiate default empty role name
				string roleName = string.Empty;
				switch (role)
				{
					// Is Administration
					case Role.Administration:
						roleName = nameof(Role.Administration);
						break;
					// Is Student
					case Role.Student:
						roleName = nameof(Role.Student);
						break;
					// Is Teacher
					case Role.Teacher:
						roleName = nameof(Role.Teacher);
						break;
					// Is General Member
					case Role.GeneralMember:
						roleName = nameof(Role.GeneralMember);
						break;
				}

				// Get role by name
				var systemRoleEntity = await _unitOfWork.Repository<SystemRole, int>()
					.GetWithSpecAsync(new BaseSpecification<SystemRole>(sr => sr.EnglishName.Equals(roleName)));
				// Is not found
				if (systemRoleEntity == null)
				{
					return new ServiceResult(ResultCodeConst.SYS_Warning0004,
						await _msgService.GetMessageAsync(ResultCodeConst.SYS_Warning0004),
						new SystemRoleDto());
				}

				// Is get success <- map to Dto
				return new ServiceResult(ResultCodeConst.SYS_Success0002,
					await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0002),
					_mapper.Map<SystemRoleDto>(systemRoleEntity));
			}
			catch (Exception ex)
			{
				_logger.Error(ex.Message);
				throw new Exception("Error invoke when progress get role by name");
			}
		}

		public async Task<IServiceResult> GetAllByRoleType(RoleType roleType)
		{
			try
			{
				// Get roles by role type
				var roles = await _unitOfWork.Repository<SystemRole, int>()
					.GetAllWithSpecAsync(new BaseSpecification<SystemRole>(
						sr => sr.RoleType.Equals(roleType.ToString())));

				if (roles.Any()) // Found roles 
				{
					return new ServiceResult(ResultCodeConst.SYS_Success0002,
						await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0002),
						_mapper.Map<List<SystemRoleDto>>(roles));
				}

				// Not found any role
				return new ServiceResult(ResultCodeConst.SYS_Warning0004,
					await _msgService.GetMessageAsync(ResultCodeConst.SYS_Success0004),
					new List<SystemRoleDto>());
			}
			catch (Exception ex)
			{
				_logger.Error(ex.Message);
				throw new Exception("Error invoke when get role");
			}
		}
	}
}
