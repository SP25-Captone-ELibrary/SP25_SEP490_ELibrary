using FPTU_ELibrary.API.Extensions;
using FPTU_ELibrary.API.Middlewares;
using FPTU_ELibrary.API.Payloads;
using FPTU_ELibrary.Application;
using FPTU_ELibrary.Application.Hubs;
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
    // Configure endpoints, swagger
    .ConfigureEndpoints()
    // Configure Serilog
    .ConfigureSerilog(builder)
    // Configure CamelCase for validation
    .ConfigureCamelCaseForValidation()
    // Configure appSettings
    .ConfigureAppSettings(builder.Configuration, builder.Environment)
    // Configure Redis
    .ConfigureRedis(builder.Configuration, builder.Environment)
    // Configure Cloudinary
    .ConfigureCloudinary(builder.Configuration)
    // Configure healthcheck 
    .ConfigureHealthCheckServices(builder.Configuration);

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
    // Add signalR
    .AddSignalR();
var app = builder.Build();

// app.UseHealthChecks($"/{APIRoute.HealthCheck.Check}");

// Register database initializer
app.Lifetime.ApplicationStarted.Register(() => Task.Run(async () =>
{
    await app.InitializeDatabaseAsync();
    await app.InitializeElasticAsync();
}));

// Configure swagger settings
if (app.Environment.IsDevelopment())
{
    app.WithSwagger();
}

app.UseHttpsRedirection();
app.UseRouting(); 
app.UseCors(policy =>
{
    policy.AllowAnyOrigin() // Cho phép mọi origin trong môi trường phát triển
        .AllowAnyMethod()
        .AllowAnyHeader();
});
app.UseAuthentication();
app.UseAuthorization(); 

// Custom Middlewares
app.UseMiddleware<ExceptionHandlingMiddleware>(); // Exception handling middleware
app.UseMiddleware<LanguageHandlingMiddleware>(); // Language handling middleware
app.UseMiddleware<PermissionMiddleware>(); // Permission middleware
// app.UseMiddleware<AuthenticationMiddleware>(); // Authentication middleware
app.MapControllers(); // Maps controller endpoints after middleware pipeline
app.UseCors(x => x
    .AllowAnyMethod()
    .AllowAnyHeader()
    .SetIsOriginAllowed(origin => true)
    .AllowCredentials());
app.UseEndpoints(ep => ep.MapApplicationHubs());
app.Run();
