using FPTU_ELibrary.Application.Configurations;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using FPTU_ELibrary.Application.Dtos.Auth;

namespace FPTU_ELibrary.Application.Utils
{
    //	Summary:
    //		This class is to provide procedures in order to generate JWT token
    public class JwtUtils
	{
		private readonly WebTokenSettings _webTokenSettings;

		public JwtUtils() {}
		public JwtUtils(WebTokenSettings webTokenSettings)
		{
			_webTokenSettings = webTokenSettings;
		}

		// Generate JWT token 
		public async Task<(string AccessToken, DateTime ValidTo)> GenerateJWTTokenAsync(AuthenticatedUserDto user)
		{
			// Get secret key
			var authSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_webTokenSettings.IssuerSigningKey));

			// Jwt Handler
			var jwtTokenHandler = new JwtSecurityTokenHandler();

			// Token claims 
			List<Claim> authClaims = new()
			{
				new Claim(ClaimTypes.Email, user.Email),
				new Claim(ClaimTypes.Role, user.RoleName),
				new Claim(JwtRegisteredClaimNames.Name, $"{user.FirstName} {user.LastName}"),
				new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
			};

			// Token descriptor 
			var tokenDescriptor = new SecurityTokenDescriptor()
			{
				// Token claims (email, role, username, id...)
				Subject = new ClaimsIdentity(authClaims),
				Expires = DateTime.UtcNow.AddMinutes(_webTokenSettings.TokenLifeTimeInMinutes),
				SigningCredentials = new SigningCredentials(
					authSigningKey, SecurityAlgorithms.HmacSha256)
			};

			// Generate token with descriptor
			var token = jwtTokenHandler.CreateToken(tokenDescriptor);
			return await Task.FromResult((jwtTokenHandler.WriteToken(token), token.ValidTo));
		}
		
		// Generate refresh token
		public string GenerateRefreshToken()
		{
			var randomNumber = new byte[64];

			using (var numberGenerator = RandomNumberGenerator.Create())
			{
				numberGenerator.GetBytes(randomNumber);
			}

			return Convert.ToBase64String(randomNumber);
		}
	}
}
