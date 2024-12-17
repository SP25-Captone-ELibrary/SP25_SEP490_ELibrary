using System.Data.Common;
using System.Reflection;
using FPTU_ELibrary.Application.Configurations;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using System.Text;
using CloudinaryDotNet;
using FluentValidation;
using FPTU_ELibrary.Application.HealthChecks;
using Mapster;
using MapsterMapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Serilog.Core;
using StackExchange.Redis;

namespace FPTU_ELibrary.API.Extensions
{
	//  Summary:
	//      This class is to configure services for presentation layer 
	public static class ServiceCollectionExtensions
    {
        public static IServiceCollection ConfigureEndpoints(this IServiceCollection services)
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
			
			// Register the Serilog logger
			services.AddSingleton(Log.Logger);

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
			// Configure CloudinarySettings
			services.Configure<CloudinarySettings>(configuration.GetSection("CloudinarySettings"));
				
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

		public static IServiceCollection ConfigureRedis(this IServiceCollection services, 
			IConfiguration configuration,
			IWebHostEnvironment env)
		{
			// Define redis configuration
			var redisConfig = env.IsDevelopment()
				? $"{configuration["RedisSettings:Host"]}:{configuration["RedisSettings:Port"]},abortConnect=false"
				: $"{Environment.GetEnvironmentVariable("REDIS_URL")},abortConnect=false";

			// Add Redis distributed caching services
			services.AddStackExchangeRedisCache(config =>
			{
				config.Configuration = redisConfig;
			});

			try
			{
				// Register IConnectionMultiplexer (used in CacheHealthCheck and custom Redis operations)
				services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect(redisConfig));
			}
			catch (RedisConnectionException ex)
			{
				Logger.None.Error("Redis connection failed: {msg}", ex.Message);
			}
			
			return services;
		}

		public static IServiceCollection ConfigureCloudinary(this IServiceCollection services,
			IConfiguration configuration)
		{
			Cloudinary cloudinary = new Cloudinary(configuration["CloudinarySettings:CloudinaryUrl"]!)
			{
				Api = { Secure = true }
			};

			services.AddSingleton(cloudinary);

			return services;
		}

		public static IServiceCollection ConfigureSignalR(this IServiceCollection services)
		{
			services.AddSignalR();
			return services;
		}

		public static IServiceCollection ConfigureHealthCheckServices(this IServiceCollection services, 
			IConfiguration configuration)
		{
			services.AddSingleton<AggregatedHealthCheckService>();
			services.AddScoped<DbConnection>(sp => 
				new SqlConnection(configuration.GetConnectionString("DefaultConnectionStr")));
			
			return services;
		}

		public static IServiceCollection ConfigureCamelCaseForValidation(this IServiceCollection services)
		{
			ValidatorOptions.Global.PropertyNameResolver = CamelCasePropertyNameResolver.ResolvePropertyName;

			return services;
		}
		
		public static IServiceCollection AddAuthentication(this IServiceCollection services, 
			IConfiguration configuration)
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
				ValidateLifetime = bool.Parse(configuration["WebTokenSettings:ValidateLifetime"]!),
				ClockSkew = TimeSpan.Zero
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

		public static IServiceCollection AddCors(this IServiceCollection services, string policyName)
		{
			// Configure CORS
			services.AddCors(p => p.AddPolicy(policyName, policy =>
			{
				// allow all with any header, method
				policy.WithOrigins("*")
					.AllowAnyHeader()
					.AllowAnyMethod();
			}));

			return services;
		}
    }
}
