using FPTU_ELibrary.Application.HealthChecks;

namespace FPTU_ELibrary.API.Extensions
{
    public static class HealthCheckExtensions
    {
        public static IHealthChecksBuilder AddSqlServerHealthCheck(this IHealthChecksBuilder builder)
        {
            // Add check for database health 
            return builder.AddCheck<DatabaseHealthCheck>("Database");
        }
        
        public static IHealthChecksBuilder AddApiHealthCheck(this IHealthChecksBuilder builder)
        {
            // Add check for database health 
            return builder.AddCheck<ApiHealthCheck>("API", tags: new[] { "api" });
        }
        
        public static IHealthChecksBuilder AddCacheHealthCheck(this IHealthChecksBuilder builder)
        {
            // Add check for database health 
            return builder.AddCheck<CacheHealthCheck>("Cache");
        }
    }
}
