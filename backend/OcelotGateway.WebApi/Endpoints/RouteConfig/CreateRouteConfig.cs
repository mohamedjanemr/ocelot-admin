using FastEndpoints;
using OcelotGateway.Application.DTOs;
using OcelotGateway.Application.Interfaces;

namespace OcelotGateway.WebApi.Endpoints.RouteConfig;

public class CreateRouteConfigRequest
{
    public string Name { get; set; } = string.Empty;
    public string DownstreamPathTemplate { get; set; } = string.Empty;
    public string UpstreamPathTemplate { get; set; } = string.Empty;
    public string UpstreamHttpMethod { get; set; } = string.Empty;
    public string DownstreamScheme { get; set; } = string.Empty;
    public List<HostAndPortDto> DownstreamHostAndPorts { get; set; } = new();
    public string Environment { get; set; } = "Development";
    public string? ServiceName { get; set; }
    public string? LoadBalancerOptions { get; set; }
    public string? AuthenticationOptions { get; set; }
    public string? RateLimitOptions { get; set; }
    public string? QoSOptions { get; set; }
}

public class CreateRouteConfigEndpoint : Endpoint<CreateRouteConfigRequest, RouteConfigDto>
{
    private readonly IRouteConfigService _routeConfigService;

    public CreateRouteConfigEndpoint(IRouteConfigService routeConfigService)
    {
        _routeConfigService = routeConfigService;
    }

    public override void Configure()
    {
        Post("/api/route-configs");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "Create new route configuration";
            s.Description = "Creates a new route configuration";
            s.Responses[201] = "Route configuration created successfully";
            s.Responses[400] = "Invalid request data";
        });
    }

    public override async Task HandleAsync(CreateRouteConfigRequest req, CancellationToken ct)
    {
        var createDto = new CreateRouteConfigDto
        {
            Name = req.Name,
            DownstreamPathTemplate = req.DownstreamPathTemplate,
            UpstreamPathTemplate = req.UpstreamPathTemplate,
            UpstreamHttpMethod = req.UpstreamHttpMethod,
            DownstreamScheme = req.DownstreamScheme,
            DownstreamHostAndPorts = req.DownstreamHostAndPorts,
            Environment = req.Environment,
            ServiceName = req.ServiceName,
            LoadBalancerOptions = req.LoadBalancerOptions,
            AuthenticationOptions = req.AuthenticationOptions,
            RateLimitOptions = req.RateLimitOptions,
            QoSOptions = req.QoSOptions
        };

        var result = await _routeConfigService.CreateRouteAsync(createDto, "system");
        
        if (result == null)
        {
            await SendErrorsAsync(400, ct);
            return;
        }

        await SendCreatedAtAsync<GetRouteConfigByIdEndpoint>(
            new { id = result.Id }, 
            result, 
            generateAbsoluteUrl: true, 
            cancellation: ct);
    }
} 