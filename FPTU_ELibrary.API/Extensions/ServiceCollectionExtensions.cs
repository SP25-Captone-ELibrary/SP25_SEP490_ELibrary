using FPTU_ELibrary.API.Payloads;
using FPTU_ELibrary.Application.Configurations;
using Microsoft.AspNetCore.Authentication.Cookies;
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
        public static IServiceCollection ConfigureServices(this IServiceCollection services, IWebHostEnvironment env)
        {
			// Add controllers
            services.AddControllers();
			// Configures ApiExplorer
			services.AddEndpointsApiExplorer();
			// Add swagger
			services.AddSwaggerGen();
			// Add Redis Cache
			services.AddStackExchangeRedisCache(config =>
			{
				config.Configuration = env.IsDevelopment()
					? "127.0.0.1:6379"
					: Environment.GetEnvironmentVariable("REDIS_URL");
			});

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
			// Configure FacebookAuthSettings
			services.Configure<FacebookAuthSettings>(configuration.GetSection("FacebookAuthSettings"));
			
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
				options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme; // For API requests
				options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme; // For login challenge
				options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme; 
			}).AddJwtBearer(options => // Enables JWT-bearer authentication
			{
				// Disable Https required for the metadata address or authority
				options.RequireHttpsMetadata = false;
				// Define type and definitions required for validating a token
				options.TokenValidationParameters = services.BuildServiceProvider()
					.GetRequiredService<TokenValidationParameters>();
			});

			return services;
		}
	}
}
