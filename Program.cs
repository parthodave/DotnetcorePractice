using Azure.Extensions.AspNetCore.Configuration.Secrets;
using Azure.Identity;
using Azure.Messaging.ServiceBus;
using Azure.Security.KeyVault.Secrets;
using DotNet8WebAPI;
using DotNet8WebAPI.Application.Books;
using DotNet8WebAPI.Helpers;
using DotNet8WebAPI.Infrastructure.Messaging;
using DotNet8WebAPI.Infrastructure.Messaging.Consumers;
using DotNet8WebAPI.Middlewares;
using DotNet8WebAPI.Model;
using DotNet8WebAPI.Services;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using Serilog;
using Serilog.Context;
using Serilog.Sinks.ApplicationInsights;

// ===== PRODUCTION-GRADE LOGGING SETUP =====
// Serilog enriches logs with context, environment, and correlation IDs
// This enables enterprise-level observability and troubleshooting
Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Environment", Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development")
    .Enrich.WithProperty("Version", "1.0.0")
    .Enrich.WithProperty("MachineName", Environment.MachineName)
    .Enrich.WithProperty("ThreadId", Environment.CurrentManagedThreadId)
    .MinimumLevel.Information()
    .WriteTo.File("Logs/log-.txt", rollingInterval: RollingInterval.Day)
    .WriteTo.ApplicationInsights(new TelemetryClient(), TelemetryConverter.Traces)
    .CreateLogger();

try
{
    Log.Information("===== APPLICATION STARTUP INITIATED =====");
}
catch (Exception ex)
{
    Log.Fatal(ex, "===== APPLICATION TERMINATED UNEXPECTEDLY =====");
    throw;
}

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplicationInsightsTelemetry();

builder.Host.UseSerilog();

// Add services to the container.
//string connectionString = builder.Configuration.GetConnectionString("OurHeroConnectionString");

//builder.Services.Configure<AppSettings>(builder.Configuration.GetSection("AppSettings"));

builder.Services.AddScoped<IOurHeroService, OurHeroService>();
builder.Services.AddScoped<IBookService, BookService>();
//builder.Services.AddSingleton<IOurHeroService, OurHeroService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddMemoryCache();


//builder.Services.AddTransient<IOurHeroService, OurHeroService>();

builder.Services.AddSwaggerGen(swagger =>
{
    //This is to generate the Default UI of Swagger Documentation  
    swagger.SwaggerDoc("v1", new OpenApiInfo
    {
        Version = "v1",
        Title = "JWT Token Authentication API",
        Description = ".NET 8 Web API"
    });
    // To Enable authorization using Swagger (JWT)  
    swagger.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme()
    {
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "JWT Authorization header using the Bearer scheme. \r\n\r\n Enter 'Bearer' [space] and then your token in the text input below.\r\n\r\nExample: \"Bearer 12345abcdef\"",
    });
    swagger.AddSecurityRequirement(new OpenApiSecurityRequirement
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

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngularApp", policy =>
    {
        policy.WithOrigins("http://localhost:4200") // Allow only this origin
              .AllowAnyHeader() // Allow any header
              .AllowAnyMethod(); // Allow any HTTP method
    });
});

builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.PropertyNamingPolicy = null; // Preserve PascalCase
});
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

//string connectionString = string.Empty;

//builder.Configuration
//    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
//    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
//    .AddEnvironmentVariables();

//connectionString = builder.Configuration["ConnectionStrings:DefaultConnection"];

//if (builder.Environment.IsProduction())
//{
//    // Configure Key Vault
//    var keyVaultUri = builder.Configuration["KeyVault:VaultUri"];
//    builder.Configuration.AddAzureKeyVault(new Uri(keyVaultUri), new DefaultAzureCredential());

//    connectionString = builder.Configuration["ConnectionString"];
//}

builder.Configuration
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
    .AddUserSecrets<Program>(optional: true)
    .AddEnvironmentVariables();

var keyVaultUrl = builder.Configuration["KeyVault:VaultUri"];

if (!string.IsNullOrWhiteSpace(keyVaultUrl))
{
    builder.Configuration.AddAzureKeyVault(
        new Uri(keyVaultUrl),
        new DefaultAzureCredential());
}

string connectionString =
    builder.Configuration.GetConnectionString("DefaultConnection");

if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException("Connection string 'DefaultConnection' was not found in configuration or Key Vault.");
}
builder.Services.AddDbContext<OurHeroDbContext>(db =>
    db.UseSqlServer(connectionString, sqlServerOptions =>
        sqlServerOptions.EnableRetryOnFailure()));

builder.Services.Configure<AppSettings>(builder.Configuration.GetSection("AppSettings"));

var serviceBusNamespace = builder.Configuration["ServiceBus:Namespace"]?.Trim();

if (string.IsNullOrWhiteSpace(serviceBusNamespace))
{
    throw new InvalidOperationException(
        "Service Bus fully qualified namespace is not configured.");
}

if (Uri.CheckHostName(serviceBusNamespace) != UriHostNameType.Dns || !serviceBusNamespace.Contains('.'))
{
    throw new InvalidOperationException(
        "Service Bus namespace must be a fully qualified host name, for example '<namespace>.servicebus.windows.net'.");
}

builder.Services.AddSingleton<ServiceBusClient>(_ =>
    new ServiceBusClient(
        serviceBusNamespace,
        new DefaultAzureCredential()));

builder.Services.AddScoped<IServiceBusService, ServiceBusService>();
builder.Services.AddHostedService<BookCommandConsumerService>();
builder.Services.AddHostedService<BookReadRequestConsumerService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<OurHeroDbContext>();
    dbContext.Database.Migrate();
}

// ===== CONFIGURE HTTP PIPELINE =====
// Enable Swagger in all environments so the API docs are available after Azure deployment.
app.UseSwagger();
app.UseSwaggerUI();
Log.Information("Swagger UI enabled in {Environment} environment", app.Environment.EnvironmentName);

app.UseHttpsRedirection();
//app.UseMiddleware<RequestLoggingMiddleware>();
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseMiddleware<CorrelationIdMiddleware>();

app.UseAuthorization();
app.UseMiddleware<JwtMiddleware>();
app.UseCors("AllowAngularApp");

// ===== HEALTH CHECK ENDPOINT =====
// Production-grade health check for load balancers and Azure monitoring
// Verifies database connectivity and app health
app.MapGet("/health", async (OurHeroDbContext dbContext, TelemetryClient telemetryClient) =>
{
    try
    {
        // Check database connectivity
        await dbContext.Database.ExecuteSqlAsync($"SELECT 1");

        var response = new
        {
            status = "Healthy",
            timestamp = DateTime.UtcNow,
            environment = app.Environment.EnvironmentName,
            version = "1.0.0"
        };

        telemetryClient.TrackEvent("HealthCheckPassed");
        Log.Information("Health check passed - Database connectivity verified");

        return Results.Ok(response);
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Health check FAILED - Database connectivity issue");
        telemetryClient.TrackEvent("HealthCheckFailed", new Dictionary<string, string> { { "Error", ex.Message } });

        return Results.Json(new
        {
            status = "Unhealthy",
            timestamp = DateTime.UtcNow,
            error = ex.Message,
            environment = app.Environment.EnvironmentName
        }, statusCode: 503);
    }
})
.WithName("HealthCheck")
.Produces(200, typeof(object))
.Produces(503, typeof(object))
.WithDisplayName("System Health Check")
.WithDescription("Returns health status of the API and database connectivity");

app.MapControllers();

app.Run();

Log.Information("===== APPLICATION STOPPED GRACEFULLY =====");
