using FPTU_ELibrary.Application.Common;
using FPTU_ELibrary.Application.Dtos;
using FPTU_ELibrary.Application.Services.IServices;
using FPTU_ELibrary.Domain.Common.Enums;
using FPTU_ELibrary.Domain.Interfaces;
using FPTU_ELibrary.Domain.Interfaces.Services;
using FPTU_ELibrary.Domain.Interfaces.Services.Base;
using FPTU_ELibrary.Domain.Specifications;
using MapsterMapper;
using SystemRole = FPTU_ELibrary.Domain.Entities.SystemRole;
namespace FPTU_ELibrary.Application.Services
{
	public class SystemRoleService : GenericService<SystemRole, SystemRoleDto, int>, 
		ISystemRoleService<SystemRoleDto>
	{
        public SystemRoleService(
	        IUnitOfWork unitOfWork,
	        IMapper mapper,
	        ICacheService cacheService) 
			: base(unitOfWork, mapper, cacheService) 
        {
            
        }

        public async Task<IServiceResult> GetByNameAsync(Role role)
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
					await _cacheService.GetMessageAsync(ResultCodeConst.SYS_Warning0004),
					new SystemRoleDto());
			}

			// Is get success <- map to Dto
			return new ServiceResult(ResultCodeConst.SYS_Success0002, 
				await _cacheService.GetMessageAsync(ResultCodeConst.SYS_Success0002), 
				_mapper.Map<SystemRoleDto>(systemRoleEntity));
		}
	}
}
