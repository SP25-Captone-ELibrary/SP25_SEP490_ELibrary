using FPTU_ELibrary.API.Extensions;
using FPTU_ELibrary.API.Payloads;
using FPTU_ELibrary.API.Payloads.Requests.Auth;
using FPTU_ELibrary.Application.Dtos.Auth;
using FPTU_ELibrary.Domain.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;
using FPTU_ELibrary.Domain.Common.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity.Data;
using System.Security.Claims;
using FPTU_ELibrary.API.Payloads.Requests.Employee;
using Microsoft.IdentityModel.Tokens;
using ChangePasswordRequest = FPTU_ELibrary.API.Payloads.Requests.Auth.ChangePasswordRequest;

namespace FPTU_ELibrary.API.Controllers
{
    [ApiController]
	public class AuthenticationController : ControllerBase
	{
		private readonly IAuthenticationService<AuthenticateUserDto> _authenticationService;
		private readonly TokenValidationParameters _tokenValidationParameters;

		public AuthenticationController(
			IAuthenticationService<AuthenticateUserDto> authenticationService,
			TokenValidationParameters tokenValidationParameters)
        {
			_authenticationService = authenticationService;
			_tokenValidationParameters = tokenValidationParameters;
        }
		
		[Authorize]
		[HttpGet(APIRoute.Authentication.CurrentUser, Name = nameof(GetCurrentUserAsync))]
		public async Task<IActionResult> GetCurrentUserAsync()
		{
			// Retrieve user email from token
			var email = User.FindFirst(ClaimTypes.Email)?.Value;
			return Ok(await _authenticationService.GetCurrentUserAsync(email ?? string.Empty));
		}
		
		[HttpPost(APIRoute.Authentication.SignIn, Name = nameof(SignInAsync))]
		public async Task<IActionResult> SignInAsync([FromBody] SignInRequest req)
		{
			return Ok(await _authenticationService.SignInAsync(req.Email));
		}
		
		[HttpPost(APIRoute.Authentication.SignInWithPassword, Name = nameof(SignInWithPasswordAsync))]
		public async Task<IActionResult> SignInWithPasswordAsync([FromBody] SignInWithPasswordRequest req)
		{
			return Ok(await _authenticationService.SignInWithPasswordAsync(req.ToAuthenticatedUser()));
		}
		
		[HttpPost(APIRoute.Authentication.SignInWithOtp, Name = nameof(SignInWithOtpAsync))]
		public async Task<IActionResult> SignInWithOtpAsync([FromBody] SignInWithOtpRequest req)
		{
			return Ok(await _authenticationService.SignInWithOtpAsync(req.Otp, req.ToAuthenticatedUser()));
		}
		
		[HttpPost(APIRoute.Authentication.SignInAsEmployee, Name = nameof(SignInAsEmployeeAsync))]
		public async Task<IActionResult> SignInAsEmployeeAsync([FromBody] SignInWithPasswordRequest req)
		{
			return Ok(await _authenticationService.SignInAsEmployeeAsync(req.ToAuthenticatedUser()));
		}
		
		[HttpPost(APIRoute.Authentication.SignInAsAdmin, Name = nameof(SignInAsAdminAsync))]
		public async Task<IActionResult> SignInAsAdminAsync([FromBody] SignInWithPasswordRequest req)
		{
			return Ok(await _authenticationService.SignInAsAdminAsync(req.ToAuthenticatedUser()));
		}
		
		[HttpPost(APIRoute.Authentication.SignInWithGoogle, Name = nameof(SignInWithGoogleAsync))]
		public async Task<IActionResult> SignInWithGoogleAsync([FromBody] GoogleAuthRequest req)
		{
			return Ok(await _authenticationService.SignInWithGoogleAsync(req.Code));
		}

		[HttpPost(APIRoute.Authentication.SignInWithFacebook, Name = nameof(SignInWithFacebook))]
		public async Task<IActionResult> SignInWithFacebook([FromBody] FacebookAuthRequest req)
		{
			return Ok(await _authenticationService.SignInWithFacebookAsync(req.AccessToken, req.ExpiresIn));
		}

		[HttpPost(APIRoute.Authentication.SignUp, Name = nameof(SignUpAsync))]
		public async Task<IActionResult> SignUpAsync([FromBody] SignUpRequest req)
		{
			return Ok(await _authenticationService.SignUpAsync(req.ToAuthenticatedUser()));
		}

		[HttpPatch(APIRoute.Authentication.ConfirmRegistration, Name = nameof(ConfirmRegistrationAsync))]
		public async Task<IActionResult> ConfirmRegistrationAsync([FromBody] ConfirmRegistrationRequest req)
		{
			return Ok(await _authenticationService.ConfirmEmailForSignUpAsync(req.Email, req.EmailVerificationCode));
		}
		
		[HttpPost(APIRoute.Authentication.ResendOtp, Name = nameof(ResendOtpForSignUpAsync))]
		public async Task<IActionResult> ResendOtpForSignUpAsync([FromBody] ResendOtpForSignUpRequest req)
		{
			return Ok(await _authenticationService.ResendOtpAsync(req.Email));
		}

		[HttpGet(APIRoute.Authentication.ForgotPassword, Name = nameof(ForgotPasswordAsync))]
		public async Task<IActionResult> ForgotPasswordAsync([FromQuery] ForgotPasswordRequest req)
		{
			return Ok(await _authenticationService.ForgotPasswordAsync(req.Email));
		}

		[HttpPatch(APIRoute.Authentication.ChangePassword, Name = nameof(ChangePasswordAsync))]
		public async Task<IActionResult> ChangePasswordAsync([FromBody] ChangePasswordRequest req)
		{
			return Ok(await _authenticationService.ChangePasswordAsync(req.Email, req.Password, req.Token));
		}
		
		[HttpPost(APIRoute.Authentication.ChangePasswordOtpVerification, Name = nameof(ChangePasswordOtpVerificationAsync))]
		public async Task<IActionResult> ChangePasswordOtpVerificationAsync([FromBody] OtpVerificationRequest req)
		{
			return Ok(await _authenticationService.VerifyChangePasswordOtpAsync(req.Email, req.Otp));
		}
		
		[HttpPatch(APIRoute.Authentication.ChangePasswordAsEmployee, Name = nameof(ChangePasswordAsEmployeeAsync))]
		public async Task<IActionResult> ChangePasswordAsEmployeeAsync([FromBody] ChangePasswordRequest req)
		{
			return Ok(await _authenticationService.ChangePasswordAsEmployeeAsync(req.Email, req.Password, req.Token));
		} 
		
		[HttpPost(APIRoute.Authentication.RefreshToken, Name = nameof(RefreshTokenAsync))]
		public async Task<IActionResult> RefreshTokenAsync([FromBody] RefreshTokenRequest req)
		{
			// Generate new token using refresh token
			return Ok(await _authenticationService.RefreshTokenAsync(req.AccessToken, req.RefreshToken));
		}

		[HttpPost(APIRoute.Authentication.EnableMfa, Name = nameof(EnableMfaAsync))]
		public async Task<IActionResult> EnableMfaAsync([FromBody] EnableMfaRequest req)
		{
			return Ok(await _authenticationService.EnableMfaAsync(req.Email));
		}
		
		[HttpPost(APIRoute.Authentication.ValidateMfa, Name = nameof(ValidateMfaAsync))]
		public async Task<IActionResult> ValidateMfaAsync([FromBody] ValidateMfaRequest req)
		{
			return Ok(await _authenticationService.ValidateMfaAsync(req.Email, req.Otp));
		}
		
		[HttpPost(APIRoute.Authentication.ValidateBackupCode, Name = nameof(ValidateMfaBackupCodeAsync))]
		public async Task<IActionResult> ValidateMfaBackupCodeAsync([FromBody] ValidateMfaBackupCodeRequest req)
		{
			return Ok(await _authenticationService.ValidateMfaBackupCodeAsync(req.Email, req.BackupCode));
		}

		[Authorize]
		[HttpPost(APIRoute.Authentication.RegenerateBackupCode, Name = nameof(RegenerateBackupCodeAsync))]
		public async Task<IActionResult> RegenerateBackupCodeAsync()
		{
			// Retrieve user email from token
			var email = User.FindFirst(ClaimTypes.Email)?.Value;
			return Ok(await _authenticationService.RegenerateMfaBackupCodeAsync(email ?? string.Empty));
		}
		
		[Authorize]
		[HttpPost(APIRoute.Authentication.RegenerateBackupCodeConfirm, Name = nameof(RegenerateBackupCodeConfirmAsync))]
		public async Task<IActionResult> RegenerateBackupCodeConfirmAsync([FromBody] RegenerateBackupConfirmRequest req)
		{
			// Retrieve user email from token
			var email = User.FindFirst(ClaimTypes.Email)?.Value;
			return Ok(await _authenticationService.ConfirmRegenerateMfaBackupCodeAsync(email ?? string.Empty, req.Otp, req.Token));
		}

		[Authorize]
		[HttpGet(APIRoute.Authentication.GetMfaBackupAsync, Name = nameof(GetMfaBackupAsyncAsync))]
		public async Task<IActionResult> GetMfaBackupAsyncAsync()
		{
			// Retrieve user email from token
			var email = User.FindFirst(ClaimTypes.Email)?.Value;
			return Ok(await _authenticationService.GetMfaBackupAsync(email ?? string.Empty));
		}

		[Authorize]
		[HttpPut(APIRoute.Authentication.UpdateProfile, Name = nameof(UpdateProfileAsync))]
		public async Task<IActionResult> UpdateProfileAsync([FromBody] UpdateProfileRequest req)
		{
			// Retrieve user email & user type from token
			var email = User.FindFirst(ClaimTypes.Email)?.Value;
			var isEmployee = User.FindFirst(CustomClaimTypes.UserType)?.Value == ClaimValues.EMPLOYEE_CLAIMVALUE;
			return Ok(await _authenticationService.UpdateProfileAsync(
				req.ToAuthenticateUserDto(email ?? string.Empty, isEmployee)));
		}
	}
}
