using FPTU_ELibrary.Domain.Interfaces;
using FPTU_ELibrary.Domain.Interfaces.Repositories.Base;
using FPTU_ELibrary.Infrastructure.Data;
using FPTU_ELibrary.Infrastructure.Data.Context;
using FPTU_ELibrary.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FPTU_ELibrary.Infrastructure
{
    public static class DependencyInjection
    {
		//	Summary:
		//		This class is to configure services for infrastructure layer
		public static IServiceCollection AddInfrastructure(this IServiceCollection services,
            IConfiguration configuration)
        {
            // Retrieve connectionStr from application configuration
            var connectionString = configuration.GetConnectionString("DefaultConnection");
            
            // Add application DbContext 
            services.AddDbContext<ElibraryDbContext>(options => options.UseSqlServer(connectionString));

            // Register DI 
            services.AddScoped<IDatabaseInitializer, DatabaseInitializer>();
            services.AddScoped(typeof(IGenericRepository<,>), typeof(GenericRepository<,>));
            services.AddScoped<IUnitOfWork, UnitOfWork>();
 
            return services;
        }
    }
}
