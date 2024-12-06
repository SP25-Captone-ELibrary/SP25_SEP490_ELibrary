using System.Security.Claims;
using FPTU_ELibrary.API.Payloads;
using FPTU_ELibrary.Application.Common;
using FPTU_ELibrary.Application.Exceptions;
using FPTU_ELibrary.Application.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using ILogger = Serilog.ILogger;

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
		private readonly ILogger _logger;

		// Contructors
		public AuthenticationMiddleware(
			RequestDelegate next,
			TokenValidationParameters tokenValidationParameters,
			ILogger logger)
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

				// Authenticate the request
				if (!await HandleAuthenticationAsync(context))
					return;

				// Proceed to the next middleware
				await _next(context);
			}
			catch // Exception invoke
			{
				throw;
			}
		}

		//	Summary:
		//		Get token from request header
		private static string GetTokenFromHeader(HttpContext context)
		{
			if (!context.Request.Headers.TryGetValue("Authorization", out var authorizationHeader))
				return string.Empty;

			var token = authorizationHeader.FirstOrDefault();
			if (string.IsNullOrWhiteSpace(token) || !token.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
				return string.Empty;

			return token["Bearer ".Length..].Trim();
		}
		
		//	Summary:
		//		Handle authenticate 
		private async Task<bool> HandleAuthenticationAsync(HttpContext context)
		{
			var token = GetTokenFromHeader(context);
			if (string.IsNullOrEmpty(token))
			{
				_logger.Warning("Authorization header missing or invalid.");
				throw new UnauthorizedException("Authorization token is missing or invalid.");
			}

			return await ValidateAccessTokenAsync(context, token);
		}
			
		//	Summary:
		//		Validate access token
		private async Task<bool> ValidateAccessTokenAsync(HttpContext context, string accessToken)
	    {
	        try
	        {
		        // Progress validate access token, read to JwtSecurityToken
		        var jwtToken = await (new JwtUtils(_tokenValidationParameters).ValidateAccessTokenAsync(accessToken));
	            // Extract claims and set user context
	            var identity = new ClaimsIdentity(jwtToken.Claims, "Bearer");
	            context.User = new ClaimsPrincipal(identity);

	            _logger.Information("Access token validated successfully.");
	            return true;
	        }
	        catch (SecurityTokenExpiredException ex)
	        {
	            // Handle token expiration
	            if (context.Request.Path.Equals($"/{APIRoute.Authentication.RefreshToken}", StringComparison.OrdinalIgnoreCase))
	            {
	                _logger.Information("Token expired. Processing refresh token request.");
	                await _next(context);
	                return true;
	            }

	            _logger.Error("Expired token: {Message}", ex.Message);
	            throw new UnauthorizedException("Token has expired.");
	        }
	        catch (SecurityTokenValidationException ex)
	        {
	            _logger.Error("Token validation failed: {Message}", ex.Message);
	            throw new UnauthorizedException("Token validation failed.");
	        }
	        catch (Exception ex)
	        {
	            _logger.Error("Unexpected error during token validation: {Message}", ex.Message);
	            throw new UnauthorizedException("Unexpected error during token validation.");
	        }
	    }
		
	}
}
