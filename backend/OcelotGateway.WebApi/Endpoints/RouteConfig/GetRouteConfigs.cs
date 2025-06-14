using FastEndpoints;
using OcelotGateway.Application.DTOs;
using OcelotGateway.Application.Interfaces;

namespace OcelotGateway.WebApi.Endpoints.RouteConfig;

public class GetRouteConfigsRequest
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}

public class GetRouteConfigsResponse
{
    public List<RouteConfigDto> RouteConfigs { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}

public class GetRouteConfigsEndpoint : Endpoint<GetRouteConfigsRequest, GetRouteConfigsResponse>
{
    private readonly IRouteConfigService _routeConfigService;

    public GetRouteConfigsEndpoint(IRouteConfigService routeConfigService)
    {
        _routeConfigService = routeConfigService;
    }

    public override void Configure()
    {
        Get("/api/route-configs");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "Get all route configurations";
            s.Description = "Retrieves a paginated list of all route configurations";
            s.Responses[200] = "Successfully retrieved route configurations";
        });
    }

    public override async Task HandleAsync(GetRouteConfigsRequest req, CancellationToken ct)
    {
        var routeConfigs = await _routeConfigService.GetAllRoutesAsync();
        
        var totalCount = routeConfigs.Count();
        var pagedConfigs = routeConfigs
            .Skip((req.Page - 1) * req.PageSize)
            .Take(req.PageSize)
            .ToList();

        Response = new GetRouteConfigsResponse
        {
            RouteConfigs = pagedConfigs,
            TotalCount = totalCount,
            Page = req.Page,
            PageSize = req.PageSize
        };
    }
} 