using FastEndpoints;
using OcelotGateway.Application.Interfaces;
using OcelotGateway.WebApi.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace OcelotGateway.WebApi.Endpoints.RouteConfig;

public class DeleteRouteConfigRequest
{
    public Guid Id { get; set; }
}

public class DeleteRouteConfigResponse
{
    public string Message { get; set; } = string.Empty;
    public bool Success { get; set; }
}

public class DeleteRouteConfigEndpoint : Endpoint<DeleteRouteConfigRequest, DeleteRouteConfigResponse>
{
    private readonly IRouteConfigService _routeConfigService;
    private readonly IHubContext<ConfigurationHub> _hubContext;

    public DeleteRouteConfigEndpoint(IRouteConfigService routeConfigService, IHubContext<ConfigurationHub> hubContext)
    {
        _routeConfigService = routeConfigService;
        _hubContext = hubContext;
    }

    public override void Configure()
    {
        Delete("/api/route-configs/{id}");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "Delete a route configuration";
            s.Description = "Deletes a route configuration by ID";
            s.Responses[200] = "Successfully deleted route configuration";
            s.Responses[404] = "Route configuration not found";
        });
    }

    public override async Task HandleAsync(DeleteRouteConfigRequest req, CancellationToken ct)
    {
        var success = await _routeConfigService.DeleteRouteAsync(req.Id);
        
        if (!success)
        {
            Response = new DeleteRouteConfigResponse
            {
                Message = "Route configuration not found",
                Success = false
            };
            await SendNotFoundAsync(ct);
            return;
        }

        // Notify clients about the deletion
        await _hubContext.Clients.All.SendAsync("RouteConfigDeleted", req.Id, ct);

        Response = new DeleteRouteConfigResponse
        {
            Message = "Route configuration deleted successfully",
            Success = true
        };
    }
} 