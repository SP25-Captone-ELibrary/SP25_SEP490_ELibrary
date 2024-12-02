using FPTU_ELibrary.API.Payloads;
using FPTU_ELibrary.Application.Configurations;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using System.Security.Claims;
using System.Text;
using FPTU_ELibrary.Domain.Common.Constants;

namespace FPTU_ELibrary.API.Extensions
{
	//  Summary:
	//      This class is to configure services for presentation layer 
	public static class ServiceCollectionExtensions
    {
        public static IServiceCollection ConfigureServices(this IServiceCollection services, IConfiguration configuration)
        {
			// Add controllers
            services.AddControllers();
			// Configures ApiExplorer
			services.AddEndpointsApiExplorer();
			// Add swagger
			services.AddSwaggerGen();

			return services;
        }

        public static IServiceCollection ConfigureSerilog(this IServiceCollection services, WebApplicationBuilder builder)
        {
			Log.Logger = new LoggerConfiguration()
	            .Enrich.FromLogContext()
	            .WriteTo.Debug()
	            .WriteTo.Console()
	            .Enrich.WithProperty("Environment", builder.Environment)
	            .ReadFrom.Configuration(builder.Configuration)
	            .CreateLogger();

			builder.Host.UseSerilog();

			return services;
		}
    
		public static IServiceCollection ConfigureAppSettings(this IServiceCollection services,
			IConfiguration configuration,
			IWebHostEnvironment env)
		{
			// Configure AppSettings
			services.Configure<AppSettings>(configuration.GetSection("AppSettings"));
			// Configure ElasticSettings
			services.Configure<ElasticSettings>(configuration.GetSection("ElasticConfiguration"));
			// Configure WebTokenSettings
			services.Configure<WebTokenSettings>(configuration.GetSection("WebTokenSettings"));
			// Configure GoogleAuthSettings
			services.Configure<GoogleAuthSettings>(configuration.GetSection("GoogleAuthSettings"));

			#region Development stage
			if (env.IsDevelopment()) // Is Development env
			{
				
			}
			#endregion
			#region Production stage
			else if (env.IsProduction()) // Is Production env
			{

			}
			#endregion
			#region Staging 
			else if (env.IsStaging()) // Is Staging env
			{

			}
			#endregion

			return services;
		}
		
		public static IServiceCollection AddAuthentication(this IServiceCollection services, IConfiguration configuration)
		{
			// Define TokenValidationParameters
			var tokenValidationParameters = new TokenValidationParameters
			{
				ValidateIssuerSigningKey = bool.Parse(configuration["WebTokenSettings:ValidateIssuerSigningKey"]!),
				IssuerSigningKey =
					new SymmetricSecurityKey(
						Encoding.UTF8.GetBytes(configuration["WebTokenSettings:IssuerSigningKey"]!)),
				ValidateIssuer = bool.Parse(configuration["WebTokenSettings:ValidateIssuer"]!),
				ValidAudience = configuration["WebTokenSettings:ValidAudience"],
				ValidIssuer = configuration["WebTokenSettings:ValidIssuer"],
				ValidateAudience = bool.Parse(configuration["WebTokenSettings:ValidateAudience"]!),
				RequireExpirationTime = bool.Parse(configuration["WebTokenSettings:RequireExpirationTime"]!),
				ValidateLifetime = bool.Parse(configuration["WebTokenSettings:ValidateLifetime"]!)
			};
			
			// Register TokenValidationParameters in the DI container
			services.AddSingleton(tokenValidationParameters);
			
			// Add authentication
			services.AddAuthentication(options =>
			{
				// Define default scheme
				options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme; // For API requests
				options.DefaultChallengeScheme = GoogleDefaults.AuthenticationScheme; // For login challenge
				options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme; // For Google sign-in
			})
			// Enables JWT-bearer authentication
			.AddJwtBearer(options =>
			{
				// Disable Https required for the metadata address or authority
				options.RequireHttpsMetadata = false;
				// Define type and definitions required for validating a token
				options.TokenValidationParameters = services.BuildServiceProvider()
					.GetRequiredService<TokenValidationParameters>();
			})
			.AddCookie(CookieAuthenticationDefaults.AuthenticationScheme)
			// Add Google authentication
			.AddGoogle(options => 
			{
				// OAuth2 ClientId
				options.ClientId = configuration["GoogleAuthSettings:ClientId"]!;
				// OAuth2 ClientSecret
				options.ClientSecret = configuration["GoogleAuthSettings:ClientSecret"]!;
				// SignIn Authentication Scheme
				options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
				// Handle authentication event CreateTicket
				options.Events.OnCreatingTicket = ctx =>
				{
					// Claim identity
					var identity = (ClaimsIdentity)ctx.Principal?.Identity! ?? new ClaimsIdentity();
					// User profile picture
					var profilePic = ctx.User.GetProperty("picture").GetString();
					// User email
					var email = ctx.User.GetProperty("email").GetString();
					// User name
					var name = ctx.User.GetProperty("name").GetString();

					// Add claims
					identity.AddClaim(new Claim("profilePic", profilePic ?? string.Empty));
					identity.AddClaim(new Claim(ClaimTypes.Email, email ?? string.Empty));
					identity.AddClaim(new Claim(ClaimTypes.Name, name ?? string.Empty));

					// Mark as completed task
					return Task.CompletedTask;
				};

			});

			return services;
		}
	}
}
