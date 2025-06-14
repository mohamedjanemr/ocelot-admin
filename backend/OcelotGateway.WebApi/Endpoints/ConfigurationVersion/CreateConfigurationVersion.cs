using FastEndpoints;
using OcelotGateway.Application.DTOs;
using OcelotGateway.Application.Interfaces;

namespace OcelotGateway.WebApi.Endpoints.ConfigurationVersion;

public class CreateConfigurationVersionRequest
{
    public string Version { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Environment { get; set; } = "Development";
    public List<Guid> RouteIds { get; set; } = new();
}

public class CreateConfigurationVersionEndpoint : Endpoint<CreateConfigurationVersionRequest, ConfigurationVersionDto>
{
    private readonly IConfigurationVersionService _configurationVersionService;

    public CreateConfigurationVersionEndpoint(IConfigurationVersionService configurationVersionService)
    {
        _configurationVersionService = configurationVersionService;
    }

    public override void Configure()
    {
        Post("/api/configuration-versions");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "Create new configuration version";
            s.Description = "Creates a new configuration version";
            s.Responses[201] = "Configuration version created successfully";
            s.Responses[400] = "Invalid request data";
        });
    }

    public override async Task HandleAsync(CreateConfigurationVersionRequest req, CancellationToken ct)
    {
        var createDto = new CreateConfigurationVersionDto
        {
            Version = req.Version,
            Description = req.Description,
            Environment = req.Environment,
            RouteIds = req.RouteIds
        };

        var result = await _configurationVersionService.CreateVersionAsync(createDto, "system");
        
        if (result == null)
        {
            await SendErrorsAsync(400, ct);
            return;
        }

        await SendOkAsync(result, ct);
    }
} 