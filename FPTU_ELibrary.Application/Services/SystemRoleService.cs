using FPTU_ELibrary.Application.Common;
using FPTU_ELibrary.Application.Dtos;
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
        public SystemRoleService(IUnitOfWork unitOfWork, IMapper mapper) 
			: base(unitOfWork, mapper) 
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
				return new ServiceResult(ResultConst.FAIL_READ_CODE, ResultConst.FAIL_READ_MSG,
					new SystemRoleDto());
			}

			// Is get success <- map to Dto
			return new ServiceResult(ResultConst.SUCCESS_READ_CODE, ResultConst.SUCCESS_READ_MSG, 
				_mapper.Map<SystemRoleDto>(systemRoleEntity));
		}
	}
}
