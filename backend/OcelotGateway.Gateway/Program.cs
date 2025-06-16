using Microsoft.EntityFrameworkCore;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using OcelotGateway.Gateway.Configuration;
using OcelotGateway.Infrastructure.Data;
using OcelotGateway.Domain.Interfaces;
using OcelotGateway.Infrastructure.Repositories;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Serilog;
using OcelotGateway.Gateway.Services; // Added for SignalRService

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog for logging
builder.Host.UseSerilog((context, config) =>
{
    config.ReadFrom.Configuration(context.Configuration);
});

// Add services to the container
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection") ?? 
                     "Data Source=ocelot_gateway.db"));

// Add repositories
builder.Services.AddScoped<IRouteConfigRepository, RouteConfigRepository>();
builder.Services.AddScoped<IConfigurationVersionRepository, ConfigurationVersionRepository>();

// Get current environment
var environment = builder.Environment.EnvironmentName;

// Add Ocelot with our custom database configuration provider
builder.Services.AddOcelot()
    .AddDatabaseConfiguration(environment);

// Add CORS for development
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Add health checks
builder.Services.AddHealthChecks();

// Register SignalRService as a hosted service
builder.Services.AddHostedService<SignalRService>();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

// Use CORS
app.UseCors("AllowAll");

// Add health check endpoint
app.MapHealthChecks("/health");

// Ensure database is created and seeded with default configuration
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await context.Database.EnsureCreatedAsync();
    
    // Seed default configuration if none exists
    await SeedDefaultConfigurationAsync(scope.ServiceProvider, environment);
}

// Use Ocelot middleware
await app.UseOcelot();

app.Run();

/// <summary>
/// Seeds the database with a default configuration if none exists
/// </summary>
static async Task SeedDefaultConfigurationAsync(IServiceProvider serviceProvider, string environment)
{
    var configRepository = serviceProvider.GetRequiredService<IConfigurationVersionRepository>();
    var routeRepository = serviceProvider.GetRequiredService<IRouteConfigRepository>();
    
    // Check if any configuration exists for this environment
    var existingConfigs = await configRepository.GetByEnvironmentAsync(environment);
    if (existingConfigs.Any())
    {
        return; // Configuration already exists
    }
    
    // Create a default configuration version
    var configVersion = new OcelotGateway.Domain.Entities.ConfigurationVersion(
        "1.0.0",
        "Initial default configuration",
        environment,
        "System"
    );
    
    // Create a default route that forwards to the admin API
    var defaultRoute = new OcelotGateway.Domain.Entities.RouteConfig(
        "Admin API Route",
        "/api/{everything}",
        "/admin-api/{everything}",
        "GET,POST,PUT,DELETE",
        "http",
        new List<OcelotGateway.Domain.ValueObjects.HostAndPort> 
        { 
            new("localhost", 5001) // Default admin API port
        },
        environment,
        "System"
    );
    
    // Add the route to the repository first
    await routeRepository.AddAsync(defaultRoute);
    
    // Add route to configuration version
    configVersion.AddRouteConfiguration(defaultRoute);
    
    // Add configuration version to repository
    await configRepository.AddAsync(configVersion);
    
    // Activate the configuration
    configVersion.Publish("System");
    await configRepository.UpdateAsync(configVersion);
    
    Log.Information("Default configuration seeded for environment: {Environment}", environment);
}

// Make the implicit Program class public so it can be referenced by tests
public partial class Program { } 
public interface IGatewayMarker{}