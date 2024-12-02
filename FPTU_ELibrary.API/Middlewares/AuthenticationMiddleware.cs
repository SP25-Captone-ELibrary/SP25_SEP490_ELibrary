using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using FPTU_ELibrary.API.Payloads;
using FPTU_ELibrary.Application.Common;
using FPTU_ELibrary.Application.Dtos;
using FPTU_ELibrary.Application.Exceptions;
using FPTU_ELibrary.Domain.Common.Constants;
using FPTU_ELibrary.Domain.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;

namespace FPTU_ELibrary.API.Middlewares
{
	//	Summary:
	//		This class is to handle authentication for the application as well as handle 
	//		refresh token for expired token
	public class AuthenticationMiddleware
	{
		// Func that can process HTTP request
		private readonly RequestDelegate _next;
		// Contains a set of parameters that are used by a SecurityTokenHandler when validating a SecurityToken.
		private readonly TokenValidationParameters _tokenValidationParameters;
		private readonly ILogger<AuthenticationMiddleware> _logger;

		// Contructors
		public AuthenticationMiddleware(
			RequestDelegate next,
			TokenValidationParameters tokenValidationParameters,
			ILogger<AuthenticationMiddleware> logger)
		{
			_next = next;
			_tokenValidationParameters = tokenValidationParameters;
			_logger = logger;
        }

		public async Task InvokeAsync(HttpContext context)
		{
			try
			{
				// Check if the endpoint allows anonymous access
				var endpoint = context.GetEndpoint();
				if (endpoint?.Metadata?.GetMetadata<IAllowAnonymous>() != null)
				{
					await _next(context); // Skip authentication and proceed to the next middleware
					return;
				}

				// Authenticate
				await HandleAuthenticateAsync(context);

				// Proceed to the next middleware
				await _next(context);
			}
			catch // Exception invoke
			{
				throw;
			}
		}

		//	Summary:
		//		Handle authenticate 
		private async Task<bool> HandleAuthenticateAsync(HttpContext context)
		{
			// Check whether request headers exist Authorization
			if (!context.Request.Headers.ContainsKey("Authorization"))
				throw new UnauthorizedException("Fail to authenticate."); // response 401
	
			// Get value of Authorization in request headers
			string authorizationHeader = context.Request.Headers["Authorization"]!;
			if(string.IsNullOrEmpty(authorizationHeader)) // Exist empty token 
				throw new UnauthorizedException("Invalid token."); // Invalid token

			// Not contains key word 'bearer'
			if (!authorizationHeader.StartsWith("bearer", StringComparison.OrdinalIgnoreCase))
				throw new UnauthorizedException("Invalid token."); // Invalid token
			
			// Get access token
			var accessToken = authorizationHeader.Substring("bearer".Length).Trim();
			if (string.IsNullOrEmpty(accessToken)) // Not found token
				throw new UnauthorizedException("Invalid token."); // Invalid token
			
			try
			{
				// Handle validate token
				return await ValidateAccessTokenAsync(context, accessToken);
			}
			catch (Exception ex) // Invoke exception
			{
				// Invalid token
				throw new UnauthorizedException(ex.Message);
			}
		}
			
		//	Summary:
		//		Validate access token
		private async Task<bool> ValidateAccessTokenAsync(HttpContext context, string accessToken)
		{
			try
			{
				// Initialize token handler
				var tokenHandler = new JwtSecurityTokenHandler();
				if (!tokenHandler.CanReadToken(accessToken)) // Determines if the token is a well formed of JWT
					throw new UnauthorizedException("Invalid token format.");
				
				// Validate token
				var validationResult =
					await tokenHandler.ValidateTokenAsync(
						token: accessToken, validationParameters: _tokenValidationParameters);
				
				if (!validationResult.IsValid) // Invalid token
					throw new UnauthorizedException("Token validation failed.");

				// Retrieve the validated token and extract claims
				var jwtToken = tokenHandler.ReadJwtToken(accessToken);

				// Create a ClaimsPrincipal with the token's claims
				var identity = new ClaimsIdentity(jwtToken.Claims, "Bearer");
				context.User = new ClaimsPrincipal(identity);
				
				return true; // Token is validated
			}
			catch (SecurityTokenExpiredException ex) // Expired token
			{
				if(context.Request.Path.Equals($"/{APIRoute.Authentication.RefreshToken}", StringComparison.OrdinalIgnoreCase))
				{
					// Continue to process the request for the refresh token endpoint
					await _next(context);
				}
				else
				{
					// Log the error and return 401 Unauthorized for other endpoints
					_logger.LogError("Token expired: {Message}", ex.Message);
					throw new UnauthorizedException("Token has been expired.");
				}
			}
			catch (SecurityTokenValidationException ex)
			{
				_logger.LogError("Validate access token failed: {0}", ex.Message);
			}

			return false; // Fail to validate token
		}
	}
}
