using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using FPTU_ELibrary.Application.Configurations;
using FPTU_ELibrary.Application.Exceptions;
using FPTU_ELibrary.Domain.Common.Constants;
using Microsoft.IdentityModel.Tokens;
using Serilog;

namespace FPTU_ELibrary.Application.Utils;

public class PaymentUtils
{
    private readonly ILogger? _logger;

    public PaymentUtils(ILogger logger)
    {
        _logger = logger;
    }
    
    /// <summary>
    /// Generate transaction token 
    /// </summary>
    /// <param name="email"></param>
    /// <param name="transactionCode"></param>
    /// <param name="transactionDate"></param>
    /// <param name="webTokenSettings"></param>
    /// <returns></returns>
    public async Task<string> GenerateTransactionTokenAsync(
        string email,
        string transactionCode, 
        DateTime transactionDate,
        WebTokenSettings webTokenSettings)
    {
        // Get secret key
        var authSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(webTokenSettings.IssuerSigningKey));

        // Jwt Handler
        var jwtTokenHandler = new JwtSecurityTokenHandler();
        
        // Token claims 
        List<Claim> authClaims = new()
        {
            // Transaction code
            new Claim(CustomClaimTypes.TransactionCode, transactionCode),
            // Transaction date
            new Claim(CustomClaimTypes.TransactionDate, transactionDate.ToString(CultureInfo.InvariantCulture)),
            // Email
            new Claim(JwtRegisteredClaimNames.Email, email),
            // Jti
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };
        
        // Token descriptor 
        var tokenDescriptor = new SecurityTokenDescriptor()
        {
            Subject = new ClaimsIdentity(authClaims),
            Expires = DateTime.UtcNow.AddMinutes(webTokenSettings.PaymentTokenLifeTimeInMinutes),
            Issuer = webTokenSettings.ValidIssuer,
            Audience = webTokenSettings.ValidAudience,
            SigningCredentials = new SigningCredentials(
                authSigningKey, SecurityAlgorithms.HmacSha256)
        };
        
        // Generate token with descriptor
        var token = jwtTokenHandler.CreateToken(tokenDescriptor);
        return await Task.FromResult(jwtTokenHandler.WriteToken(token));
    }

    /// <summary>
    /// Validate transaction token using TokenValidationParameters
    /// </summary>
    /// <param name="token"></param>
    /// <param name="tokenValidationParameters"></param>
    /// <returns></returns>
    /// <exception cref="UnauthorizedException"></exception>
    public async Task<JwtSecurityToken?> ValidateTransactionTokenAsync(
        string token, TokenValidationParameters tokenValidationParameters)
    {
        // Initialize token handler
        var tokenHandler = new JwtSecurityTokenHandler();

        // Check if the token format is valid
        if (!tokenHandler.CanReadToken(token))
            throw new UnauthorizedException("Invalid token format.");
        
        try
        {
            // Validate token
            var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out var validatedToken);

            // Ensure the token is a JWT
            if (validatedToken is not JwtSecurityToken jwtToken)
                throw new UnauthorizedException("Invalid token type.");

            // Ensure the algorithm used matches the expected algorithm
            if (!jwtToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
                throw new UnauthorizedException("Invalid token algorithm.");

            // Check for Transaction token claim
            // Transaction code
            var transactionCodeClaim = principal.FindFirst(c => c.Type == CustomClaimTypes.TransactionCode);
            if (transactionCodeClaim == null)
                throw new UnauthorizedException("Transaction code claim is missing or invalid.");
            
            // Transaction date
            var transactionDateClaim = principal.FindFirst(c => c.Type == CustomClaimTypes.TransactionDate);
            if (transactionDateClaim == null)
            {
                throw new UnauthorizedException("Transaction date claim is missing or invalid.");
            }
            
            // Email
            var emailClaim = principal.FindFirst(c => c.Type == ClaimTypes.Email);
            if (emailClaim == null || string.IsNullOrEmpty(emailClaim.Value))
                throw new UnauthorizedException("Email claim is missing or invalid.");
            
            // Mark as complete
            await Task.CompletedTask;
            // Mark as valid token
            return jwtToken;
        }
        catch (SecurityTokenException ex)
        {
            if(_logger != null) _logger.Error(ex.Message);
            // Handle token validation errors
            throw new UnauthorizedException("Token validation failed: " + ex.Message);
        }
    }

    /// <summary>
    /// Extract all transaction information in token
    /// </summary>
    /// <param name="token"></param>
    /// <returns></returns>
    public (string Email, string TransactionCode, DateTime TransactionDate)
        ExtractTransactionDataFromToken(JwtSecurityToken token)
    {
        string email = token.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Email)?.Value ?? string.Empty;
        string transactionCode = token.Claims.FirstOrDefault(c => c.Type == CustomClaimTypes.TransactionCode)?.Value ?? string.Empty;
    
        DateTime transactionDate = DateTime.MinValue;
        if (DateTime.TryParse(
                token.Claims.FirstOrDefault(c => c.Type == CustomClaimTypes.TransactionDate)?.Value,
                out DateTime validDate))
        {
            transactionDate = validDate;
        }

        return (email, transactionCode, transactionDate);
    }
    
    public static string GenerateRequestId()
    {
        return $"RE{GenerateRandomDigits(11)}";
    }

    public static int GenerateRandomOrderCodeDigits(int length)
    {
        return int.Parse(GenerateRandomDigitsWithTimeStamp(length));
    }

    public static string GenerateOrderId(string requestId)
    {
        if (!string.IsNullOrEmpty(requestId) 
            && requestId.Length > 2 
            && requestId.StartsWith("RE"))
        {
            string uniqueDigits = requestId.Substring(2);
            return $"OD{uniqueDigits}";
        }

        return Guid.NewGuid().ToString();
    }
    
    // Generate random digits
    public static string GenerateRandomDigits(int length)
    {
        var rnd = new Random();
        string digits = string.Empty;
        
        for(int i = 0; i < length; ++i)
        {
            digits += rnd.Next(0, 10); // Random each digit from 0 and 9
        }

        return digits;
    }
    
    private static string GenerateRandomDigitsWithTimeStamp(int length)
    {
        var rnd = new Random();
    
        // Get a timestamp (ticks)
        long timestamp = DateTime.Now.Ticks;
    
        // Use the last part of the timestamp to ensure limited size 
        string timestampPart = timestamp.ToString().Substring(timestamp.ToString().Length - Math.Min(8, length));

        // Generate the random digits portion
        string digits = string.Empty;
        for (int i = 0; i < length - timestampPart.Length; ++i)
        {
            digits += rnd.Next(0, 10); 
        }

        // Combine random digits with timestamp part
        return digits + timestampPart;
    }
}