using System.IdentityModel.Tokens.Jwt;
using FPTU_ELibrary.API.Extensions;
using FPTU_ELibrary.API.Payloads;
using FPTU_ELibrary.API.Payloads.Requests.Auth;
using FPTU_ELibrary.Application.Common;
using FPTU_ELibrary.Application.Dtos.Auth;
using FPTU_ELibrary.Domain.Interfaces.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using FPTU_ELibrary.API.Payloads.Requests;
using FPTU_ELibrary.Application.Exceptions;
using FPTU_ELibrary.Domain.Common.Constants;
using Microsoft.AspNetCore.Authentication.Facebook;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity.Data;

using ChangePasswordRequest = FPTU_ELibrary.API.Payloads.Requests.Auth.ChangePasswordRequest;

namespace FPTU_ELibrary.API.Controllers
{
    [ApiController]
	public class AuthenticationController : ControllerBase
	{
		private readonly IAuthenticationService<AuthenticateUserDto> _authenticationService;

		public AuthenticationController(
			IAuthenticationService<AuthenticateUserDto> authenticationService)
        {
			_authenticationService = authenticationService;
		}

		[AllowAnonymous]
		[HttpPost(APIRoute.Authentication.SignIn, Name = nameof(SignInAsync))]
		public async Task<IActionResult> SignInAsync([FromBody] SignInRequest req)
		{
			return Ok(await _authenticationService.SignInAsync(req.Email));
		}
		
		[AllowAnonymous]
		[HttpPost(APIRoute.Authentication.SignInWithPassword, Name = nameof(SignInWithPasswordAsync))]
		public async Task<IActionResult> SignInWithPasswordAsync([FromBody] SignInWithPasswordRequest req)
		{
			return Ok(await _authenticationService.SignInWithPasswordAsync(req.ToAuthenticatedUser()));
		}
		
		[AllowAnonymous]
		[HttpPost(APIRoute.Authentication.SignInWithOtp, Name = nameof(SignInWithOtpAsync))]
		public async Task<IActionResult> SignInWithOtpAsync([FromBody] SignInWithOtpRequest req)
		{
			return Ok(await _authenticationService.SignInWithOtpAsync(req.Otp, req.ToAuthenticatedUser()));
		}

		[AllowAnonymous]
		[HttpPost(APIRoute.Authentication.OtpVerification, Name = nameof(OtpVerificationAsync))]
		public async Task<IActionResult> OtpVerificationAsync([FromBody] OtpVerificationRequest req)
		{
			return Ok(await _authenticationService.VerifyOtpAsync(req.Email, req.Otp));
		}
		
		[AllowAnonymous]
		[HttpPost(APIRoute.Authentication.SignInAsEmployee, Name = nameof(SignInAsEmployeeAsync))]
		public async Task<IActionResult> SignInAsEmployeeAsync([FromBody] SignInRequest req)
		{
			return Ok(await _authenticationService.SignInAsEmployeeAsync(req.ToAuthenticatedUser()));
		}
		
		[AllowAnonymous]
		[HttpPost(APIRoute.Authentication.SignInWithGoogle, Name = nameof(SignInWithGoogleAsync))]
		public async Task<IActionResult> SignInWithGoogleAsync([FromBody] GoogleAuthRequest req)
		{
			return Ok(await _authenticationService.SignInWithGoogleAsync(req.Code));
		}

		[AllowAnonymous]
		[HttpPost(APIRoute.Authentication.SignInWithFacebook, Name = nameof(SignInWithFacebook))]
		public async Task<IActionResult> SignInWithFacebook([FromBody] FacebookAuthRequest req)
		{
			return Ok(await _authenticationService.SignInWithFacebookAsync(req.AccessToken, req.ExpiresIn));
		}

		[AllowAnonymous]
		[HttpPost(APIRoute.Authentication.SignUp, Name = nameof(SignUpAsync))]
		public async Task<IActionResult> SignUpAsync([FromBody] SignUpRequest req)
		{
			// Progress create new user with in-active status
			var serviceResult = await _authenticationService.SignUpAsync(req.ToAuthenticatedUser());

			// Progress response
			return serviceResult.ResultCode == ResultCodeConst.SYS_Success0001 // Create successfully
					? Created()
					: Ok(serviceResult);
		}

		[AllowAnonymous]
		[HttpPatch(APIRoute.Authentication.ConfirmRegistration, Name = nameof(ConfirmRegistrationAsync))]
		public async Task<IActionResult> ConfirmRegistrationAsync([FromBody] ConfirmRegistrationRequest req)
		{
			return Ok(await _authenticationService.ConfirmEmailAsync(req.Email, req.EmailVerificationCode));
		}

		[HttpPost(APIRoute.Authentication.RefreshToken, Name = nameof(RefreshTokenAsync))]
		public async Task<IActionResult> RefreshTokenAsync([FromBody] RefreshTokenRequest req)
		{
			// Retrieve claims from the authenticated user's identity
			var roleName = User.FindFirst("role")?.Value;
			var userType = User.FindFirst(CustomClaimTypes.UserType)?.Value;
			var email = User.FindFirst(JwtRegisteredClaimNames.Email)?.Value;
			var name = User.FindFirst(JwtRegisteredClaimNames.Name)?.Value;
			var tokenId = User.FindFirst(JwtRegisteredClaimNames.Jti)?.Value;
			if (string.IsNullOrEmpty(email) // Is not exist email claim
			    || string.IsNullOrEmpty(userType) // Is not exist user type claim
			    || string.IsNullOrEmpty(roleName) // Is not exist role claim
			    || string.IsNullOrEmpty(name) // Is not exist name claim
			    || string.IsNullOrEmpty(tokenId)) // Is not exist tokenId claim
			{
				// 401
				throw new UnauthorizedException("Missing token claims.");
			}
			
			// Generate new token using refresh token
			return Ok(await _authenticationService.RefreshTokenAsync(
				email: email,
				userType: userType,
				name: name,
				roleName: roleName,
				tokenId: tokenId,
				refreshToken: req.RefreshToken));
		}

		[AllowAnonymous]
		[HttpGet(APIRoute.Authentication.ForgotPassword, Name = nameof(ForgotPasswordAsync))]
		public async Task<IActionResult> ForgotPasswordAsync([FromQuery] ForgotPasswordRequest req)
		{
			return Ok(await _authenticationService.ForgotPasswordAsync(req.Email));
		}

		[AllowAnonymous]
		[HttpPost(APIRoute.Authentication.ChangePassword, Name = nameof(ChangePasswordAsync))]
		public async Task<IActionResult> ChangePasswordAsync([FromBody] ChangePasswordRequest req)
		{
			return Ok(await _authenticationService.ChangePasswordAsync(req.Email, req.Password, req.Token));
		}
		
		[AllowAnonymous]
		[HttpPost(APIRoute.Authentication.ChangePasswordAsEmployee, Name = nameof(ChangePasswordAsEmployeeAsync))]
		public async Task<IActionResult> ChangePasswordAsEmployeeAsync([FromBody] ChangePasswordRequest req)
		{
			return Ok(await _authenticationService.ChangePasswordAsEmployeeAsync(req.Email, req.Password, req.Token));
		}
	}
}
