using FPTU_ELibrary.Application.Dtos;
using FPTU_ELibrary.Domain.Entities;
using Mapster;

namespace FPTU_ELibrary.API.Mappings
{
	public class MappingRegistration : IRegister
	{
		public void Register(TypeAdapterConfig config)
		{
			config.NewConfig<Book, BookDto>();
			config.NewConfig<User, UserDto>();
			config.NewConfig<Employee, EmployeeDto>();
			config.NewConfig<RefreshToken, RefreshTokenDto>();
			config.NewConfig<SystemMessage, SystemMessageDto>();
		}
	}
}
