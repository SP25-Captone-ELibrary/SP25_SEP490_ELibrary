using FPTU_ELibrary.Application.Services.IServices;
using FPTU_ELibrary.Domain.Interfaces;

namespace FPTU_ELibrary.API.Extensions
{
    public static class InitializeExtensions
    {
        // Summary:
        //      Progress initialize database (Migrate, Seed Data)
        public static async Task InitializeDatabaseAsync(this WebApplication app)
        {
            // Create IServiceScope to resolve scoped services
            using (var scope = app.Services.CreateScope())
            {
                // Get service typeof IFptuLibraryDbContextInitilizer from IServiceProvider
                var initializer = scope.ServiceProvider.GetRequiredService<IDatabaseInitializer>();

                // Initialize database (if not exist)
                await initializer.InitializeAsync();

                // Seeding default data 
                await initializer.SeedAsync();
            }
        }

        //  Summary:
        //  Progress intialize elastic index, documents
        public static async Task InitializeElasticAsync(this WebApplication app)
        {
			// Create IServiceScope to resolve scoped services
			using (var scope = app.Services.CreateScope())
			{
				// Get service typeof IElasticInitializeService from IServiceProvider
				var initializer = scope.ServiceProvider.GetRequiredService<IElasticInitializeService>();

				// Create index and Indexing documents if not exist
				await initializer.RunAsync();
			}
		}
    }
}
