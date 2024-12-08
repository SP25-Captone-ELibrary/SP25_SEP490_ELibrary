using FPTU_ELibrary.API.Payloads.Requests.Auth;
using FPTU_ELibrary.API.Payloads.Requests.Employee;
using FPTU_ELibrary.Application.Dtos;
using FPTU_ELibrary.Application.Dtos.Auth;
using FPTU_ELibrary.Domain.Common.Enums;
using Org.BouncyCastle.Ocsp;

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

		#region User
		// Mapping from typeof(CreateUserRequest) to typeof(UserDto)
		public static UserDto ToUser(this CreateUserRequest req)
			=> new UserDto
			{
				Email = req.Email,
				FirstName = req.FirstName,
				LastName = req.LastName,
			};

		// Mapping from typeof(UpdateUserRequest) to typeof(UserDto)
		public static UserDto ToUserForUpdate(this UpdateUserRequest req)
			=> new UserDto()
			{
				FirstName = req.FirstName,
				LastName = req.LastName,
				Dob = req.Dob,
				Phone = req.Phone,
			};

		// Mapping from typeof(UpdateUserRequest) to typeof(UserDto)
		public static UserDto ToUpdateRoleUser(this UpdateUserRequest req)
			=> new UserDto()
			{ 
				UserCode = req.UserCode,
				RoleId = req.RoleId??4,
			};
		#endregion

		#region Employee
		// Mapping from typeof(CreateEmployeeRequest) to typeof(EmployeeDto)
		public static EmployeeDto ToEmployeeDtoForCreate(this CreateEmployeeRequest req)
			=> new EmployeeDto()
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
				CreateDate = DateTime.UtcNow,
				IsActive = false,
				EmailConfirmed = false,
				PhoneNumberConfirmed = false,
				TwoFactorEnabled = false
			};

		#endregion
	}
}
