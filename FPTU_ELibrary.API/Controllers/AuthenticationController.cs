using System.IdentityModel.Tokens.Jwt;
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
using FPTU_ELibrary.Domain.Common.Constants;
using Microsoft.AspNetCore.Authorization;
using Nest;

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
			return Ok(await _authenticationService.SignInAsync(req.ToAuthenticatedUser(), 
				isSignInFromExternalProvider: false));
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
			// User surname 
			var surname = claims?.FirstOrDefault(c => c.Type == ClaimTypes.Surname)?.Value;
			// User given name
			var givenName = claims?.FirstOrDefault(c => c.Type == ClaimTypes.GivenName)?.Value;

			// Initialize userDto
			var authenticatedUser = new AuthenticateUserDto()
			{
				Email = email!,
				Avatar = profilePic,
				// Reverse fullname based on vietnamese naming conventions rules
				FirstName = givenName ?? string.Empty,
				LastName = surname ?? string.Empty,
			};

			return Ok(await _authenticationService.SignInAsync(authenticatedUser, 
					// Mark as sign-up with google
					isSignInFromExternalProvider: true));
		}

		[AllowAnonymous]
		[HttpPost(APIRoute.Authentication.SignUp, Name = nameof(SignUpAsync))]
		public async Task<IActionResult> SignUpAsync([FromBody] SignUpRequest req)
		{
			// Progress create new user with in-active status
			var serviceResult = await _authenticationService.SignUpAsync(req.ToAuthenticatedUser());

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
	}
}
