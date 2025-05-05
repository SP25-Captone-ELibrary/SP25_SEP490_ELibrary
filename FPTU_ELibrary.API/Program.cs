using FPTU_ELibrary.API.Extensions;
using FPTU_ELibrary.API.Middlewares;
using FPTU_ELibrary.Application;
using FPTU_ELibrary.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    // Add HttpClient
    .AddHttpClient()
    // Add CORS
    .AddCors("Cors")
    // Add system health checks
    .AddHealthChecks()
    // For SQL
    .AddSqlServerHealthCheck()
    // For API
    .AddApiHealthCheck()
    // For memory cache
    .AddCacheHealthCheck();

builder.Services
    // Configure background services
    .ConfigureBackgroundServices()
    // Configure endpoints, swagger
    .ConfigureEndpoints()
    // Configure Serilog
    .ConfigureSerilog(builder)
    // Configure CamelCase for validation
    .ConfigureCamelCaseForValidation()
    // Configure appSettings
    .ConfigureAppSettings(builder, builder.Environment)
    // Configure Redis
    .ConfigureRedis(builder.Configuration, builder.Environment)
    // Configure Cloudinary
    .ConfigureCloudinary(builder.Configuration)
    // Configure HealthCheck 
    .ConfigureHealthCheckServices(builder.Configuration)
    // Configure OCR
    .ConfigureOCR(builder.Configuration)
    // Configure Azure Speech
    .ConfigureAzureSpeech(builder.Configuration)
    // Establish application configuration based on env
    .EstablishApplicationConfiguration(builder.Configuration, builder.Environment);   

builder.Services
    // Configure for application layer
    .AddApplication(builder.Configuration)
    // Configure for infrastructure layer
    .AddInfrastructure(builder.Configuration);

builder.Services
    // Add swagger
    .AddSwagger()
    // Add authentication
    .AddAuthentication(builder.Configuration)
    // Add Lazy resolution
    .AddLazyResolution()
    // Add signalR
    .AddSignalR();

var app = builder.Build();

// Register database initializer
app.Lifetime.ApplicationStarted.Register(() => Task.Run(async () =>
{
    await app.InitializeDatabaseAsync();
    await app.InitializeElasticAsync();
}));

app.WithSwagger();
app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

// Only apply azure configuration for production env
if(builder.Environment.IsProduction()) app.UseAzureAppConfiguration();

// Custom Middlewares
app.UseMiddleware<ExceptionHandlingMiddleware>(); // Exception handling middleware
app.UseMiddleware<LanguageHandlingMiddleware>(); // Language handling middleware
app.UseMiddleware<PermissionMiddleware>(); // Permission middleware

app.MapControllers(); // Maps controller endpoints after middleware pipeline
app.UseCors(x => x
    .AllowAnyMethod()
    .AllowAnyHeader()
    .SetIsOriginAllowed(origin => true)
    .AllowCredentials());
app.UseEndpoints(ep => ep.MapApplicationHubs());
app.Run();