using FastEndpoints;
using OcelotGateway.Application.DTOs;
using OcelotGateway.Application.Interfaces;
using OcelotGateway.WebApi.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace OcelotGateway.WebApi.Endpoints.RouteConfig;

public class UpdateRouteConfigRequest
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string DownstreamPathTemplate { get; set; } = string.Empty;
    public string UpstreamPathTemplate { get; set; } = string.Empty;
    public string UpstreamHttpMethod { get; set; } = string.Empty;
    public string DownstreamScheme { get; set; } = "http";
    public List<HostAndPortDto> DownstreamHostAndPorts { get; set; } = new();
    public string? ServiceName { get; set; }
    public string? LoadBalancerOptions { get; set; }
    public string? AuthenticationOptions { get; set; }
    public string? RateLimitOptions { get; set; }
    public string? QoSOptions { get; set; }
    public string Environment { get; set; } = "Development";
}

public class UpdateRouteConfigResponse
{
    public RouteConfigDto RouteConfig { get; set; } = new();
    public string Message { get; set; } = string.Empty;
}

public class UpdateRouteConfigEndpoint : Endpoint<UpdateRouteConfigRequest, UpdateRouteConfigResponse>
{
    private readonly IRouteConfigService _routeConfigService;
    private readonly IHubContext<ConfigurationHub> _hubContext;

    public UpdateRouteConfigEndpoint(IRouteConfigService routeConfigService, IHubContext<ConfigurationHub> hubContext)
    {
        _routeConfigService = routeConfigService;
        _hubContext = hubContext;
    }

    public override void Configure()
    {
        Put("/api/route-configs/{id}");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "Update a route configuration";
            s.Description = "Updates an existing route configuration";
            s.Responses[200] = "Successfully updated route configuration";
            s.Responses[404] = "Route configuration not found";
        });
    }

    public override async Task HandleAsync(UpdateRouteConfigRequest req, CancellationToken ct)
    {
        var updateDto = new UpdateRouteConfigDto
        {
            Name = req.Name,
            DownstreamPathTemplate = req.DownstreamPathTemplate,
            UpstreamPathTemplate = req.UpstreamPathTemplate,
            UpstreamHttpMethod = req.UpstreamHttpMethod,
            DownstreamScheme = req.DownstreamScheme,
            DownstreamHostAndPorts = req.DownstreamHostAndPorts,
            ServiceName = req.ServiceName,
            LoadBalancerOptions = req.LoadBalancerOptions,
            AuthenticationOptions = req.AuthenticationOptions,
            RateLimitOptions = req.RateLimitOptions,
            QoSOptions = req.QoSOptions
        };

        var updatedRouteConfig = await _routeConfigService.UpdateRouteAsync(req.Id, updateDto, "system");
        
        if (updatedRouteConfig == null)
        {
            await SendNotFoundAsync(ct);
            return;
        }

        // Notify clients about the update
        await _hubContext.Clients.All.SendAsync("RouteConfigUpdated", updatedRouteConfig, ct);

        Response = new UpdateRouteConfigResponse
        {
            RouteConfig = updatedRouteConfig,
            Message = "Route configuration updated successfully"
        };
    }
} 