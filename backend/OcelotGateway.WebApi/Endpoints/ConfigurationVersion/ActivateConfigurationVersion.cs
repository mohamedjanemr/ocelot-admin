using FastEndpoints;
using OcelotGateway.Application.DTOs;
using OcelotGateway.Application.Interfaces;
using OcelotGateway.WebApi.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace OcelotGateway.WebApi.Endpoints.ConfigurationVersion;

public class ActivateConfigurationVersionRequest
{
    public Guid Id { get; set; }
}

public class ActivateConfigurationVersionResponse
{
    public ConfigurationVersionDto ConfigurationVersion { get; set; } = new();
    public string Message { get; set; } = string.Empty;
    public bool Success { get; set; }
}

public class ActivateConfigurationVersionEndpoint : Endpoint<ActivateConfigurationVersionRequest, ActivateConfigurationVersionResponse>
{
    private readonly IConfigurationVersionService _configurationVersionService;
    private readonly IHubContext<ConfigurationHub> _hubContext;

    public ActivateConfigurationVersionEndpoint(
        IConfigurationVersionService configurationVersionService,
        IHubContext<ConfigurationHub> hubContext)
    {
        _configurationVersionService = configurationVersionService;
        _hubContext = hubContext;
    }

    public override void Configure()
    {
        Post("/api/configuration-versions/{id}/activate");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "Activate a configuration version";
            s.Description = "Activates a specific configuration version, making it the current active configuration";
            s.Responses[200] = "Successfully activated configuration version";
            s.Responses[404] = "Configuration version not found";
            s.Responses[400] = "Configuration version is invalid or cannot be activated";
        });
    }

    public override async Task HandleAsync(ActivateConfigurationVersionRequest req, CancellationToken ct)
    {
        try
        {
            var success = await _configurationVersionService.PublishVersionAsync(req.Id, "system");
            
            if (!success)
            {
                Response = new ActivateConfigurationVersionResponse
                {
                    Message = "Configuration version not found or cannot be activated",
                    Success = false
                };
                await SendNotFoundAsync(ct);
                return;
            }

            // Get the activated version for response
            var activatedVersion = await _configurationVersionService.GetVersionByIdAsync(req.Id);

            // Notify all clients about the configuration change
            await _hubContext.Clients.All.SendAsync("ConfigurationVersionActivated", activatedVersion, ct);

            Response = new ActivateConfigurationVersionResponse
            {
                ConfigurationVersion = activatedVersion!,
                Message = "Configuration version activated successfully",
                Success = true
            };
        }
        catch (Exception ex)
        {
            Response = new ActivateConfigurationVersionResponse
            {
                Message = $"Error activating configuration version: {ex.Message}",
                Success = false
            };
            await SendAsync(Response, 400, ct);
        }
    }
} 