using FPTU_ELibrary.API.Payloads.Requests.Auth;
using FPTU_ELibrary.Application.Dtos;
using FPTU_ELibrary.Application.Dtos.Auth;
using Org.BouncyCastle.Ocsp;

namespace FPTU_ELibrary.API.Extensions
{
	// Summary:
	//		This class provide extensions method mapping from request payload to specific 
	//		application objects
	public static class PayloadExtensions
	{
		// Mapping from typeof(AuthenticationRequest) to typeof(AuthenticatedUserDto)
		public static AuthenticatedUserDto ToAuthenticatedUser(this AuthenticationRequest req)
			=> new AuthenticatedUserDto
			{
				Email = req.Email,
				Password = req.Password
			};

		// Mapping from typeof(SignUpRequest) to typeof(AuthenticatedUserDto)
		public static AuthenticatedUserDto ToAuthenticatedUser(this SignUpRequest req)
			=> new AuthenticatedUserDto
			{
				UserCode = req.UserCode,
				Email = req.Email,
				FirstName = req.FirstName,
				LastName = req.LastName,
				Password = req.Password,
				IsEmployee = false
			};

		public static UserDto ToUser(this CreateUserRequest req)
			=> new UserDto
			{
				Email = req.Email,
				FirstName = req.FirstName,
				LastName = req.LastName,
			};

		public static UserDto ToUserForUpdate(this UpdateUserRequest req)
			=> new UserDto()
			{
				FirstName = req.FirstName,
				LastName = req.LastName,
				Dob = req.Dob,
				Phone = req.Phone,
			};

		public static UserDto ToUpdateRoleUser(this UpdateUserRequest req)
			=> new UserDto()
			{ 
				UserCode = req.UserCode,
				RoleId = req.RoleId??4,
			};

	}
}
