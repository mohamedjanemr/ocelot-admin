using FastEndpoints;
using OcelotGateway.Application.Interfaces;
using Microsoft.EntityFrameworkCore;
using OcelotGateway.Infrastructure.Data;

namespace OcelotGateway.WebApi.Endpoints.Health;

public class HealthCheckResponse
{
    public string Status { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public Dictionary<string, object> Services { get; set; } = new();
    public string Version { get; set; } = string.Empty;
    public int TotalRouteConfigs { get; set; }
    public int ActiveRouteConfigs { get; set; }
    public int TotalConfigurationVersions { get; set; }
}

public class HealthCheckEndpoint : EndpointWithoutRequest<HealthCheckResponse>
{
    private readonly ApplicationDbContext _context;
    private readonly IRouteConfigService _routeConfigService;
    private readonly IConfigurationVersionService _configurationVersionService;

    public HealthCheckEndpoint(
        ApplicationDbContext context,
        IRouteConfigService routeConfigService,
        IConfigurationVersionService configurationVersionService)
    {
        _context = context;
        _routeConfigService = routeConfigService;
        _configurationVersionService = configurationVersionService;
    }

    public override void Configure()
    {
        Get("/health");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "Health check endpoint";
            s.Description = "Returns the health status of the API and its dependencies";
            s.Responses[200] = "System is healthy";
            s.Responses[503] = "System is unhealthy";
        });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var services = new Dictionary<string, object>();
        var overallHealthy = true;

        try
        {
            // Check database connectivity
            var canConnect = await _context.Database.CanConnectAsync(ct);
            services["Database"] = new { Status = canConnect ? "Healthy" : "Unhealthy", LastChecked = DateTime.UtcNow };
            if (!canConnect) overallHealthy = false;

            // Get route statistics
            var allRoutes = await _routeConfigService.GetAllRoutesAsync();
            var activeRoutes = allRoutes.Count(r => r.IsActive);
            
            // Get configuration version statistics
            var allVersions = await _configurationVersionService.GetAllVersionsAsync();

            services["RouteConfigurations"] = new { 
                Status = "Healthy", 
                Total = allRoutes.Count(), 
                Active = activeRoutes,
                LastChecked = DateTime.UtcNow 
            };

            services["ConfigurationVersions"] = new { 
                Status = "Healthy", 
                Total = allVersions.Count(),
                LastChecked = DateTime.UtcNow 
            };

            Response = new HealthCheckResponse
            {
                Status = overallHealthy ? "Healthy" : "Unhealthy",
                Timestamp = DateTime.UtcNow,
                Services = services,
                Version = "1.0.0",
                TotalRouteConfigs = allRoutes.Count(),
                ActiveRouteConfigs = activeRoutes,
                TotalConfigurationVersions = allVersions.Count()
            };

            if (!overallHealthy)
            {
                await SendAsync(Response, 503, ct);
            }
        }
        catch (Exception ex)
        {
            services["Error"] = new { Status = "Unhealthy", Message = ex.Message, LastChecked = DateTime.UtcNow };
            
            Response = new HealthCheckResponse
            {
                Status = "Unhealthy",
                Timestamp = DateTime.UtcNow,
                Services = services,
                Version = "1.0.0"
            };

            await SendAsync(Response, 503, ct);
        }
    }
} 