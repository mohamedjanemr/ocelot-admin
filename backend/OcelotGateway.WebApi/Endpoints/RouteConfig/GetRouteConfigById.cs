using FastEndpoints;
using OcelotGateway.Application.DTOs;
using OcelotGateway.Application.Interfaces;

namespace OcelotGateway.WebApi.Endpoints.RouteConfig;

public class GetRouteConfigByIdRequest
{
    public Guid Id { get; set; }
}

public class GetRouteConfigByIdEndpoint : Endpoint<GetRouteConfigByIdRequest, RouteConfigDto>
{
    private readonly IRouteConfigService _routeConfigService;

    public GetRouteConfigByIdEndpoint(IRouteConfigService routeConfigService)
    {
        _routeConfigService = routeConfigService;
    }

    public override void Configure()
    {
        Get("/api/route-configs/{id}");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "Get route configuration by ID";
            s.Description = "Retrieves a specific route configuration by its ID";
            s.Responses[200] = "Successfully retrieved route configuration";
            s.Responses[404] = "Route configuration not found";
        });
    }

    public override async Task HandleAsync(GetRouteConfigByIdRequest req, CancellationToken ct)
    {
        var routeConfig = await _routeConfigService.GetRouteByIdAsync(req.Id);
        
        if (routeConfig == null)
        {
            await SendNotFoundAsync(ct);
            return;
        }

        Response = routeConfig;
    }
} 