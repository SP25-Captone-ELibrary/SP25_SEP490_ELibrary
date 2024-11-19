using FPTU_ELibrary.API.Configurations;
using Serilog;

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
	}
}
