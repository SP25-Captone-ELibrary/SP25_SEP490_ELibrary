using FPTU_ELibrary.API.Extensions;
using FPTU_ELibrary.API.Middlewares;
using FPTU_ELibrary.Application;
using FPTU_ELibrary.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHealthChecks()
    .AddSqlServerHealthCheck();

// Configure application services
builder.Services
    .ConfigureServices(builder.Configuration)
    .ConfigureSerilog(builder)
    .ConfigureAppSettings(builder.Configuration, builder.Environment);

// Configure infrastructure services
builder.Services
    .AddApplication(builder.Configuration)
    .AddInfrastructure(builder.Configuration);

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

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();
