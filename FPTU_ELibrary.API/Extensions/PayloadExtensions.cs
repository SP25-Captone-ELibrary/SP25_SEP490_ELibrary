using FPTU_ELibrary.API.Payloads.Requests.Auth;
using FPTU_ELibrary.Application.Dtos;
using FPTU_ELibrary.Application.Dtos.Auth;

namespace FPTU_ELibrary.API.Extensions
{
	// Summary:
	//		This class provide extensions method mapping from request payload to specific 
	//		application objects
	public static class PayloadExtensions
	{
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
	}
}
