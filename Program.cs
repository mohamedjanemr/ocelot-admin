using Microsoft.EntityFrameworkCore;
using OcelotGateway.Application.Interfaces;
using OcelotGateway.Application.Services;
using OcelotGateway.Domain.Interfaces;
using OcelotGateway.Infrastructure.Data;
using OcelotGateway.Infrastructure.Repositories;
using OcelotGateway.WebApi.Hubs;

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

// Add controllers
builder.Services.AddControllers();

// Add SignalR
builder.Services.AddSignalR();

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

// Enable CORS
app.UseCors("AllowAll");

app.UseHttpsRedirection();

// Map controllers
app.MapControllers();

// Map SignalR hub
app.MapHub<ConfigurationHub>("/configurationHub");

// Add a simple test endpoint
app.MapGet("/test", () => "Hello World!");

// Ensure database is created
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    context.Database.EnsureCreated();
}

app.Run(); 