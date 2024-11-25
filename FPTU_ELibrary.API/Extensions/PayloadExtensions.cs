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
	}
}
