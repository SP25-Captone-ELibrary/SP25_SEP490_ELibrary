using Microsoft.Extensions.DependencyInjection;
using FPTU_ELibrary.Application.Services;
using FPTU_ELibrary.Domain.Interfaces.Services;
using Mapster;
using MapsterMapper;
using System.Reflection;
using System.Text;
using Microsoft.Extensions.Configuration;
using Elasticsearch.Net;
using Nest;
using FPTU_ELibrary.Application.Services.IServices;
using FPTU_ELibrary.Domain.Interfaces.Services.Base;
using FPTU_ELibrary.Application.Dtos;
using FPTU_ELibrary.Application.Dtos.Auth;
using FPTU_ELibrary.Application.Dtos.Employees;
using FPTU_ELibrary.Application.Dtos.Roles;
using FPTU_ELibrary.Application.Hubs;
using Microsoft.AspNetCore.SignalR;
using OfficeOpenXml;

namespace FPTU_ELibrary.Application
{
    public static class DependencyInjection
    {
		//	Summary:
		//		This class is to configure services for application layer
        public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration configuration)
        {
			// Register external services 
			services.AddScoped<IEmailService, EmailService>();
			services.AddScoped<ISearchService, SearchService>();
			services.AddScoped<ICacheService, CacheService>();
			services.AddScoped<ICloudinaryService, CloudinaryService>();
			services.AddScoped<ISystemMessageService, SystemMessageService>();	
			services.AddScoped<IAuthorizationService, AuthorizationService>();
			services.AddScoped<IElasticInitializeService, ElasticInitializeService>();
			
			// Register application services
			services.AddScoped(typeof(IGenericService<,,>), typeof(GenericService<,,>));
			services.AddScoped(typeof(IReadOnlyService<,,>), typeof(ReadOnlyService<,,>));
			services.AddScoped<IAuthenticationService<AuthenticateUserDto>, AuthenticationService>();
			services.AddScoped<IBookService<BookDto>, BookService>();
			services.AddScoped<IEmployeeService<EmployeeDto>, EmployeeService>();
			services.AddScoped<IUserService<UserDto>, UserService>();
			services.AddScoped<IRefreshTokenService<RefreshTokenDto>, RefreshTokenService>();	
			services.AddScoped<INotificationService<NotificationDto>, NotificationService>();	
			services.AddScoped<ISystemRoleService<SystemRoleDto>, SystemRoleService>();	
			services.AddScoped<INotificationRecipientService<NotificationRecipientDto>, NotificationRecipientService>();
            services.AddScoped<ISystemFeatureService<SystemFeatureDto>, SystemFeatureService>();
            services.AddScoped<ISystemPermissionService<SystemPermissionDto>, SystemPermissionService>();
            services.AddScoped<IRolePermissionService<RolePermissionDto>, RolePermissionService>();

            services
                .ConfigureMapster() // Add mapster
				.ConfigureCloudinary() // Add cloudinary
				.ConfigureElastic(configuration); // Add elastic
            
			//Add License for Excel handler
			ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
			// Add Hub provider
			services.AddSingleton<IUserIdProvider, UserHubProvider>();
			return services;
        }

		public static IServiceCollection ConfigureMapster(this IServiceCollection services)
		{
			TypeAdapterConfig.GlobalSettings.Default
				.MapToConstructor(true)
				.PreserveReference(true);
			// Get Mapster GlobalSettings
			var typeAdapterConfig = TypeAdapterConfig.GlobalSettings;
			// Scans the assembly and gets the IRegister, adding the registration to the TypeAdapterConfig
			typeAdapterConfig.Scan(Assembly.GetExecutingAssembly());
			
			// Register the mapper as Singleton service for my application
			var mapperConfig = new Mapper(typeAdapterConfig);
			services.AddSingleton<IMapper>(mapperConfig);

			return services;
		}

		public static IServiceCollection ConfigureCloudinary(this IServiceCollection services)
		{
			// Configure this later...

			return services;
		}
		
		public static IServiceCollection ConfigureElastic(this IServiceCollection services, IConfiguration configuration)
		{ 
			var elasticConfiguration = configuration.GetSection("ElasticSettings");

			// Access default configuration props
			var url = elasticConfiguration["Url"];
			var index = elasticConfiguration["DefaultIndex"];
			var username = elasticConfiguration["Username"];
			var password = elasticConfiguration["Password"];

			// Add elastic client settings
			var settings = new ConnectionSettings(new Uri(url!))
			   .DefaultIndex(index)
			   .ServerCertificateValidationCallback(CertificateValidations.AllowAll)
			   .BasicAuthentication(username, password);

			// Specifies how field name are inferred from CLR property names
			settings.DefaultFieldNameInferrer(p => ToSnakeCase(p));

			// Register DI for elastic search client 
			services.AddSingleton<IElasticClient>(
				// Initialize elastic client 
				new ElasticClient(settings));

			return services;
		}

		private static string ToSnakeCase(string s)
		{
			var builder = new StringBuilder(s.Length);
			for (int i = 0; i < s.Length; i++)
			{
				var c = s[i];
				if (char.IsUpper(c))
				{
					if (i == 0)
						builder.Append(char.ToLowerInvariant(c));
					else if (char.IsUpper(s[i - 1]))
						builder.Append(char.ToLowerInvariant(c));
					else
					{
						builder.Append("_");
						builder.Append(char.ToLowerInvariant(c));
					}
				}
				else
					builder.Append(c);
			}

			return builder.ToString();
		}
	}
}
