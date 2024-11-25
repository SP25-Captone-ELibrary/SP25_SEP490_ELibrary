using FPTU_ELibrary.API.Extensions;
using FPTU_ELibrary.API.Middlewares;
using FPTU_ELibrary.Application;
using FPTU_ELibrary.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services
	// Add system health checks
	.AddHealthChecks() 
    // For SQL
    .AddSqlServerHealthCheck();

builder.Services
    // Configure system services
    .ConfigureServices(builder.Configuration)
    // Configure Serilog
    .ConfigureSerilog(builder)
    // Configure appSettings
    .ConfigureAppSettings(builder.Configuration, builder.Environment);

builder.Services
	// Configure for application layer
	.AddApplication(builder.Configuration)
	// Configure for infrastructure layer
	.AddInfrastructure(builder.Configuration);

builder.Services
    // Add swagger
    .AddSwagger()
    // Add authentication
    .AddAuthentication(builder.Configuration);

var app = builder.Build();

//app.UseHealthChecks();

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
app.UseAuthentication();
app.UseAuthorization(); 

// Custom Middlewares
app.UseMiddleware<ExceptionHandlingMiddleware>(); // Exception handling middleware
app.UseMiddleware<AuthenticationMiddleware>(); // Authentication middleware

app.MapControllers(); // Maps controller endpoints after middleware pipeline
app.Run();
