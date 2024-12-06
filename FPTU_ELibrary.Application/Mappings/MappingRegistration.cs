using FPTU_ELibrary.Application.Dtos;
using FPTU_ELibrary.Domain.Entities;
using Mapster;

namespace FPTU_ELibrary.Application.Mappings
{
	public class MappingRegistration : IRegister
	{
		public void Register(TypeAdapterConfig config)
		{
			// From [Entity] to [Dto]
			config.NewConfig<Book, BookDto>();
			config.NewConfig<User, UserDto>();
			config.NewConfig<Employee, EmployeeDto>();
			config.NewConfig<RefreshToken, RefreshTokenDto>();
			config.NewConfig<RolePermission, RolePermissionDto>();
			config.NewConfig<SystemRole, SystemRoleDto>();
			config.NewConfig<SystemMessage, SystemMessageDto>();
			config.NewConfig<SystemFeature, SystemFeatureDto>();
			config.NewConfig<SystemPermission, SystemPermissionDto>();
			
			// From [Dto] to [Entity]
			config.NewConfig<UserDto, User>()
				.Ignore(dest => dest.Role)
				.IgnoreNullValues(false);
		}
	}
}
