using FPTU_ELibrary.API.Payloads.Requests;
using FPTU_ELibrary.API.Payloads.Requests.Auth;
using FPTU_ELibrary.API.Payloads.Requests.Author;
using FPTU_ELibrary.API.Payloads.Requests.Employee;
using FPTU_ELibrary.API.Payloads.Requests.Fine;
using FPTU_ELibrary.API.Payloads.Requests.Role;
using FPTU_ELibrary.Application.Dtos;
using FPTU_ELibrary.Application.Dtos.Auth;
using FPTU_ELibrary.Application.Dtos.Authors;
using FPTU_ELibrary.Application.Dtos.Employees;
using FPTU_ELibrary.Application.Dtos.Fine;
using FPTU_ELibrary.Application.Dtos.Roles;
using FPTU_ELibrary.Domain.Common.Enums;

namespace FPTU_ELibrary.API.Extensions
{
	// Summary:
	//		This class provide extensions method mapping from request payload to specific 
	//		application objects
	public static class PayloadExtensions
	{
		#region Auth
		// Mapping from typeof(SignInRequest) to typeof(AuthenticateUserDto)
		public static AuthenticateUserDto ToAuthenticatedUser(this SignInRequest req)
			=> new AuthenticateUserDto
			{
				Email = req.Email,
				Password = null!
			};
		
		// Mapping from typeof(SignInWithOtpRequest) to typeof(AuthenticateUserDto)
		public static AuthenticateUserDto ToAuthenticatedUser(this SignInWithOtpRequest req)
			=> new AuthenticateUserDto
			{
				Email = req.Email,
				Password = null!
			};
		
		// Mapping from typeof(SignInWithPasswordRequest) to typeof(AuthenticateUserDto)
		public static AuthenticateUserDto ToAuthenticatedUser(this SignInWithPasswordRequest req)
			=> new AuthenticateUserDto
			{
				Email = req.Email,
				Password = req.Password
			};

		// Mapping from typeof(SignUpRequest) to typeof(AuthenticateUserDto)
		public static AuthenticateUserDto ToAuthenticatedUser(this SignUpRequest req)
			=> new AuthenticateUserDto
			{
				UserCode = req.UserCode,
				Email = req.Email,
				FirstName = req.FirstName,
				LastName = req.LastName,
				Password = req.Password,
				IsEmployee = false
			};
		#endregion

		#region Author
		// Mapping from typeof(CreateAuthorRequest) to typeof(AuthorDto)
		public static AuthorDto ToAuthorDto(this CreateAuthorRequest req)
		{
			// Current local datetime
			var currentLocalDateTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow,
				// Vietnam timezone
				TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"));

			return new AuthorDto()
			{
				AuthorCode = req.AuthorCode,
				AuthorImage = req.AuthorImage,
				FullName = req.FullName,
				Biography = req.Biography,
				Dob = req.Dob,
				DateOfDeath = req.DateOfDeath,
				Nationality = req.Nationality,
				CreateDate = currentLocalDateTime,
				IsDeleted = false
			};
		}		
		
		// Mapping from typeof(UpdateAuthorRequest) to typeof(AuthorDto)
		public static AuthorDto ToAuthorDto(this UpdateAuthorRequest req)
		{
			// Current local datetime
			var currentLocalDateTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow,
				// Vietnam timezone
				TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"));
			
			return new AuthorDto()
			{
				AuthorCode = req.AuthorCode,
				AuthorImage = req.AuthorImage,
				FullName = req.FullName,
				Biography = req.Biography,
				Dob = req.Dob,
				DateOfDeath = req.DateOfDeath,
				Nationality = req.Nationality,
				UpdateDate = currentLocalDateTime,
				IsDeleted = false
			};
		}		
		
		#endregion
		
		#region User
		// Mapping from typeof(CreateUserRequest) to typeof(UserDto)
		public static UserDto ToUser(this CreateUserRequest req)
		{
			// Current local datetime
			var currentLocalDateTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow,
				// Vietnam timezone
				TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"));
			
			return new UserDto
			{
				UserCode = req.UserCode,
				Email = req.Email,
				FirstName = req.FirstName,
				LastName = req.LastName,
				Dob = req.Dob,
				Phone = req.Phone,
				Address = req.Address,
				Gender = req.Gender.ToString(),

				// Set default authorization values
				CreateDate = currentLocalDateTime,
				IsActive = false,
				EmailConfirmed = false,
				PhoneNumberConfirmed = false,
				TwoFactorEnabled = false
			};
		}

		// Mapping from typeof(UpdateUserRequest) to typeof(UserDto)
		public static UserDto ToUserForUpdate(this UpdateUserRequest req)
		{
			// Current local datetime
			var currentLocalDateTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow,
				// Vietnam timezone
				TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"));
			
			return new UserDto
			{
				UserCode = req.UserCode,
				FirstName = req.FirstName,
				LastName = req.LastName,
				Dob = req.Dob,
				Phone = req.Phone,
				Address = req.Address,
				Gender = req.Gender.ToString(),
				ModifiedDate = currentLocalDateTime
			};
		}

		#endregion

		#region Employee
		// Mapping from typeof(CreateEmployeeRequest) to typeof(EmployeeDto)
		public static EmployeeDto ToEmployeeDtoForCreate(this CreateEmployeeRequest req)
		{
			// Current local datetime
			var currentLocalDateTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow,
				// Vietnam timezone
				TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"));
			
			return new EmployeeDto()
			{
				EmployeeCode = req.EmployeeCode,
				Email = req.Email,
				FirstName = req.FirstName,
				LastName = req.LastName,
				Dob = req.Dob,
				Phone = req.Phone,
				Address = req.Address,
				Gender = req.Gender.ToString(),
				HireDate = req.HireDate,
				RoleId = req.RoleId,

				// Set default authorization values
				CreateDate = currentLocalDateTime,
				IsActive = false,
				EmailConfirmed = false,
				PhoneNumberConfirmed = false,
				TwoFactorEnabled = false
			};
		}
		
		public static EmployeeDto ToEmployeeDtoForUpdate(this UpdateRequest req)
		{
			// Current local datetime
			var currentLocalDateTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow,
				// Vietnam timezone
				TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"));
			
			return new EmployeeDto()
			{
				EmployeeCode = req.EmployeeCode,
				FirstName = req.FirstName,
				LastName = req.LastName,
				Dob = req.Dob,
				Phone = req.Phone,
				Address = req.Address,
				Gender = req.Gender.ToString(),
				HireDate = req.HireDate,
				TerminationDate = req.TerminationDate,
				ModifiedDate = currentLocalDateTime
			};
		}

		public static EmployeeDto ToEmployeeDtoForUpdateProfile(this UpdateProfileRequest req)
		{
			// Current local datetime
			var currentLocalDateTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow,
				// Vietnam timezone
				TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"));
			
			return new EmployeeDto()
			{
				FirstName = req.FirstName,
				LastName = req.LastName,
				Dob = req.Dob,
				Phone = req.Phone,
				Address = req.Address,
				Gender = req.Gender.ToString(),
				ModifiedDate = currentLocalDateTime,
				Avatar = req.Avatar
			};
		}

		#endregion

		#region Role

		public static SystemRoleDto ToSystemRoleDto(this UpdateRoleRequest req, int roleId)
			=> new SystemRoleDto()
			{
				RoleId = roleId,
				EnglishName = req.EnglishName,
				VietnameseName = req.VietnameseName,
				RoleType = ((Role)req.RoleTypeIdx).ToString()
			};

		#endregion
		#region BookCategory

		public static BookCategoryDto ToBookCategoryForUpdate(this UpdateBookCategoryRequest req)
		{
			return new BookCategoryDto()
			{
				VietnameseName = req.VietnameseName??"",
				EnglishName = req.EnglishName??"",
				Description = req.Description
			};
		}
		#endregion

		#region FinePolicy	
		public static FinePolicyDto ToFinePolicyDto(this CreateFinePolicyRequest req)
		{
			return new FinePolicyDto()
			{
				ConditionType = req.ConditionType,
				FineAmountPerDay = req.FineAmountPerDay,
				FixedFineAmount = req.FixedFineAmount,
				Description = req.Description
			};
		}
		
		#endregion
		
	}
}
