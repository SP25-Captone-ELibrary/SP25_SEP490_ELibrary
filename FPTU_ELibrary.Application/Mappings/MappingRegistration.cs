using FPTU_ELibrary.Application.Dtos;
using FPTU_ELibrary.Application.Dtos.Auth;
using FPTU_ELibrary.Application.Dtos.Authors;
using FPTU_ELibrary.Application.Dtos.Books;
using FPTU_ELibrary.Application.Dtos.Employees;
using FPTU_ELibrary.Application.Dtos.Notifications;
using FPTU_ELibrary.Application.Dtos.Roles;
using FPTU_ELibrary.Domain.Entities;
using Mapster;

namespace FPTU_ELibrary.Application.Mappings
{
	public class MappingRegistration : IRegister
	{
		public void Register(TypeAdapterConfig config)
		{
			// From [Entity] to [Dto]
			config.NewConfig<Author, AuthorDto>();
			config.NewConfig<Book, BookDto>();
			config.NewConfig<BookReview, BookReviewDto>();
			config.NewConfig<BookEdition, BookEditionDto>();
			config.NewConfig<BookEditionAuthor, BookEditionAuthorDto>();
			config.NewConfig<BookEditionInventory, BookEditionInventoryDto>();
			config.NewConfig<User, UserDto>();
			config.NewConfig<Employee, EmployeeDto>();
			config.NewConfig<RefreshToken, RefreshTokenDto>();
			config.NewConfig<RolePermission, RolePermissionDto>();
			config.NewConfig<SystemRole, SystemRoleDto>();
			config.NewConfig<SystemMessage, SystemMessageDto>();
			config.NewConfig<SystemFeature, SystemFeatureDto>();
			config.NewConfig<SystemPermission, SystemPermissionDto>();
			config.NewConfig<Notification, NotificationDto>();
			config.NewConfig<NotificationRecipient, NotificationRecipientDto>();
			
			// From [Dto] to [Entity]
			config.NewConfig<UserDto, User>()
				.Ignore(dest => dest.Role)
				.IgnoreNullValues(false);
			config.NewConfig<EmployeeDto, Employee>()
				.Ignore(dest => dest.EmployeeId)
				.Ignore(dest => dest.Role)
				.IgnoreNullValues(false);
			config.NewConfig<AuthorDto, Author>()
				.Ignore(dest => dest.AuthorId);
			config.NewConfig<UserDto, User>()
				.Ignore(dest => dest.UserId);
		}
	}
}
