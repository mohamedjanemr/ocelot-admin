using Microsoft.EntityFrameworkCore;
using OcelotGateway.Application.Interfaces;
using OcelotGateway.Application.Services;
using OcelotGateway.Domain.Interfaces;
using OcelotGateway.Infrastructure.Data;
using OcelotGateway.Infrastructure.Repositories;
using OcelotGateway.WebApi.Hubs;
using FastEndpoints;
using FastEndpoints.Swagger;

var builder = WebApplication.CreateBuilder(args);

// Add Entity Framework
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection") ?? 
                     "Data Source=ocelot_gateway.db"));

// Add repositories
builder.Services.AddScoped<IRouteConfigRepository, RouteConfigRepository>();
builder.Services.AddScoped<IConfigurationVersionRepository, ConfigurationVersionRepository>();

// Add application services
builder.Services.AddScoped<IRouteConfigService, RouteConfigService>();
builder.Services.AddScoped<IConfigurationVersionService, ConfigurationVersionService>();

// Add FastEndpoints
builder.Services.AddFastEndpoints();

// Add FastEndpoints Swagger
builder.Services.SwaggerDocument(o =>
{
    o.DocumentSettings = s =>
    {
        s.Title = "Ocelot Gateway Admin API";
        s.Version = "v1";
        s.Description = "API for managing Ocelot Gateway configurations";
    };
});

// Add SignalR
builder.Services.AddSignalR();

// Add CORS - Fixed for SignalR compatibility
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:3000", "http://localhost:5173", "http://localhost:5174")
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwaggerGen();
}

// Enable CORS - Use the new policy name
app.UseCors("AllowFrontend");

// app.UseHttpsRedirection();

// Configure FastEndpoints
app.UseFastEndpoints();

// Map SignalR hub
app.MapHub<ConfigurationHub>("/configurationHub");

// Ensure database is created
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    context.Database.EnsureCreated();
}

app.Run();
