using Microsoft.OpenApi.Models;

namespace FPTU_ELibrary.API.Extensions
{
	// Summary:
	//		This class is to add swagger configuration for the system
    public static class SwaggerFeatureExtensions
    {
        public static IServiceCollection AddSwagger(this IServiceCollection services)
        {
			services.AddSwaggerGen(c =>
			{
				c.SwaggerDoc("v1", new OpenApiInfo { Title = "FPTU_ELibraryManagement_API", Version = "v1" });

				c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme()
				{
					Name = "Authorization",
					Type = SecuritySchemeType.ApiKey,
					Scheme = "Bearer",
					BearerFormat = "JWT",
					In = ParameterLocation.Header,
					Description = "JWT Authorization header using the Bearer scheme. \r\n\r\n" +
					"Enter 'Bearer' [space] and then your token in the text input below.\r\n\r\nExample: \"Bearer 1safsfsdfdfd\"",
				});
				c.AddSecurityRequirement(new OpenApiSecurityRequirement
				{
					{
						new OpenApiSecurityScheme
						{
							Reference = new OpenApiReference
							{
								Type = ReferenceType.SecurityScheme,
								Id = "Bearer"
							}
						},
						new string[] {}
					}
				});
			});

			return services;
		}

        public static WebApplication WithSwagger(this WebApplication app)
        { 
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json",
                    "FPTU_ELibraryManagement API V1");
            });

            return app;
        }
    }
}
