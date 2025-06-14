using FastEndpoints;
using OcelotGateway.Application.DTOs;
using OcelotGateway.Application.Interfaces;

namespace OcelotGateway.WebApi.Endpoints.ConfigurationVersion;

public class GetConfigurationVersionsRequest
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}

public class GetConfigurationVersionsResponse
{
    public List<ConfigurationVersionDto> ConfigurationVersions { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}

public class GetConfigurationVersionsEndpoint : Endpoint<GetConfigurationVersionsRequest, GetConfigurationVersionsResponse>
{
    private readonly IConfigurationVersionService _configurationVersionService;

    public GetConfigurationVersionsEndpoint(IConfigurationVersionService configurationVersionService)
    {
        _configurationVersionService = configurationVersionService;
    }

    public override void Configure()
    {
        Get("/api/configuration-versions");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "Get all configuration versions";
            s.Description = "Retrieves a paginated list of all configuration versions";
            s.Responses[200] = "Successfully retrieved configuration versions";
        });
    }

    public override async Task HandleAsync(GetConfigurationVersionsRequest req, CancellationToken ct)
    {
        var versions = await _configurationVersionService.GetAllVersionsAsync();
        
        var totalCount = versions.Count();
        var pagedVersions = versions
            .Skip((req.Page - 1) * req.PageSize)
            .Take(req.PageSize)
            .ToList();

        Response = new GetConfigurationVersionsResponse
        {
            ConfigurationVersions = pagedVersions,
            TotalCount = totalCount,
            Page = req.Page,
            PageSize = req.PageSize
        };
    }
} 