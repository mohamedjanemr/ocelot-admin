using FastEndpoints;
using OcelotGateway.Application.DTOs;
using OcelotGateway.Application.Interfaces;

namespace OcelotGateway.WebApi.Endpoints.ConfigurationVersion;

public class GetConfigurationVersionByIdRequest
{
    public Guid Id { get; set; }
}

public class GetConfigurationVersionByIdResponse
{
    public ConfigurationVersionDto ConfigurationVersion { get; set; } = new();
}

public class GetConfigurationVersionByIdEndpoint : Endpoint<GetConfigurationVersionByIdRequest, GetConfigurationVersionByIdResponse>
{
    private readonly IConfigurationVersionService _configurationVersionService;

    public GetConfigurationVersionByIdEndpoint(IConfigurationVersionService configurationVersionService)
    {
        _configurationVersionService = configurationVersionService;
    }

    public override void Configure()
    {
        Get("/api/configuration-versions/{id}");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "Get configuration version by ID";
            s.Description = "Retrieves a specific configuration version by its ID";
            s.Responses[200] = "Successfully retrieved configuration version";
            s.Responses[404] = "Configuration version not found";
        });
    }

    public override async Task HandleAsync(GetConfigurationVersionByIdRequest req, CancellationToken ct)
    {
        var configurationVersion = await _configurationVersionService.GetVersionByIdAsync(req.Id);
        
        if (configurationVersion == null)
        {
            await SendNotFoundAsync(ct);
            return;
        }

        Response = new GetConfigurationVersionByIdResponse
        {
            ConfigurationVersion = configurationVersion
        };
    }
} 