using FPTU_ELibrary.API.Extensions;
using FPTU_ELibrary.API.Payloads;
using FPTU_ELibrary.API.Payloads.Requests.Auth;
using FPTU_ELibrary.Application.Common;
using FPTU_ELibrary.Application.Dtos.Auth;
using FPTU_ELibrary.Domain.Interfaces.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using FPTU_ELibrary.Application.Exceptions;
using Microsoft.AspNetCore.Authorization;

namespace FPTU_ELibrary.API.Controllers
{
    [ApiController]
	public class AuthenticationController : ControllerBase
	{
		private readonly IAuthenticationService<AuthenticatedUserDto> _authenticationService;

		public AuthenticationController(
			IAuthenticationService<AuthenticatedUserDto> authenticationService)
        {
			_authenticationService = authenticationService;
		}

		/// <summary>
		/// Authenticates a user and provides a JWT token upon successful sign-in.
		/// </summary>
		/// <param name="req">The authentication request containing email and password.</param>
		/// <response code="200">Returns the authentication response with a JWT token.</response>
		/// <response code="401">Unauthorized: Invalid credentials.</response>
		/// <response code="422">Unprocessable Entity: Validation errors.</response>
		[HttpPost(APIRoute.Authentication.SignIn, Name = nameof(SignInAsync))]
		[AllowAnonymous]
		public async Task<IActionResult> SignInAsync([FromBody] AuthenticationRequest req)
		{
			return Ok(await _authenticationService.SignInAsync(req.ToAuthenticatedUser()));
		}

		[AllowAnonymous]
		[HttpGet(APIRoute.Authentication.SignInWithGoogle, Name = nameof(SignInWithGoogle))]
		public IActionResult SignInWithGoogle()
		{
			var properties = new AuthenticationProperties
			{
				RedirectUri = $"/{APIRoute.Authentication.GoogleCallback}"
			};

			// Create specified ChallengeResults based on specified scheme
			return Challenge(properties, GoogleDefaults.AuthenticationScheme);
		}

		[AllowAnonymous]
		[HttpGet(APIRoute.Authentication.GoogleCallback, Name = nameof(GoogleCallbackAsync))]
		public async Task<IActionResult> GoogleCallbackAsync()
		{
			// Authenticate current request based on specific scheme
			var result = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);

			if (!result.Succeeded) // Authenticate fail
			{
				return Unauthorized();
			}

			// Get claims identity
			var claims = result.Principal.Identities.FirstOrDefault()?.Claims;
			// User email
			var email = claims?.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
			// User avatar
			var profilePic = claims?.FirstOrDefault(c => c.Type == "profilePic")?.Value;

			// Initialize userDto
			var authenticatedUser = new AuthenticatedUserDto()
			{
				Email = email!,
				Avatar = profilePic
			};

			return Ok(await _authenticationService.SignUpAsync(authenticatedUser, // Progress create (if not exist)
					// Mark as sign-up with google
					isSignUpFromExternal: true));
		}

		[AllowAnonymous]
		[HttpPost(APIRoute.Authentication.SignUp, Name = nameof(SignUpAsync))]
		public async Task<IActionResult> SignUpAsync([FromBody] SignUpRequest req)
		{
			// Progress create new user with in-active status
			var serviceResult = await _authenticationService.SignUpAsync(
				req.ToAuthenticatedUser(), isSignUpFromExternal: false);

			// Progress response
			return serviceResult.Status == ResultConst.SUCCESS_INSERT_CODE // Create successfully
					? Created()
					: StatusCode(StatusCodes.Status500InternalServerError);
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
			// Retrieve the email claim from the authenticated user's identity
			var email = User.FindFirst(ClaimTypes.Email)?.Value;
			if (string.IsNullOrEmpty(email)) // Is not exist email claim
			{
				return Unauthorized("Invalid or missing email claim.");
			}
			
			// Generate new token using refresh token
			return Ok(await _authenticationService.RefreshTokenAsync(email, req.RefreshToken));
		}
	}
}
