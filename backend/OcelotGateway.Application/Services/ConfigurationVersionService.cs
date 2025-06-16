using OcelotGateway.Application.DTOs;
using OcelotGateway.Application.Interfaces;
using OcelotGateway.Domain.Entities;
using OcelotGateway.Domain.Interfaces;
using System.Text.Json;

namespace OcelotGateway.Application.Services;

public class ConfigurationVersionService : IConfigurationVersionService
{
    private readonly IConfigurationVersionRepository _versionRepository;
    private readonly IRouteConfigRepository _routeRepository;

    public ConfigurationVersionService(
        IConfigurationVersionRepository versionRepository,
        IRouteConfigRepository routeRepository)
    {
        _versionRepository = versionRepository;
        _routeRepository = routeRepository;
    }

    public async Task<IEnumerable<ConfigurationVersionDto>> GetAllVersionsAsync()
    {
        var versions = await _versionRepository.GetAllAsync();
        var result = new List<ConfigurationVersionDto>();

        foreach (var version in versions)
        {
            var dto = MapToDto(version);
            result.Add(dto);
        }

        return result;
    }

    public async Task<ConfigurationVersionDto?> GetVersionByIdAsync(Guid id)
    {
        var version = await _versionRepository.GetByIdAsync(id);
        return version != null ? MapToDto(version) : null;
    }

    public async Task<ConfigurationVersionDto?> GetByVersionAsync(string version, string environment)
    {
        var configVersion = await _versionRepository.GetByVersionAsync(version, environment);
        return configVersion != null ? MapToDto(configVersion) : null;
    }

    public async Task<ConfigurationVersionDto?> GetActiveConfigurationAsync(string environment)
    {
        var version = await _versionRepository.GetActiveConfigurationAsync(environment);
        return version != null ? MapToDto(version) : null;
    }

    public async Task<IEnumerable<ConfigurationVersionDto>> GetByEnvironmentAsync(string environment)
    {
        var versions = await _versionRepository.GetByEnvironmentAsync(environment);
        return versions.Select(MapToDto);
    }

    public async Task<ConfigurationVersionDto> CreateVersionAsync(CreateConfigurationVersionDto createDto, string createdBy)
    {
        var version = new ConfigurationVersion(
            createDto.Version,
            createDto.Description ?? string.Empty,
            createDto.Environment,
            createdBy);

        // Add routes to this version if specified
        if (createDto.RouteIds.Any())
        {
            foreach (var routeId in createDto.RouteIds)
            {
                var route = await _routeRepository.GetByIdAsync(routeId);
                if (route != null && route.IsActive)
                {
                    version.AddRouteConfiguration(route);
                }
            }
        }
        else
        {
            // If no specific routes provided, use all active routes from the environment
            var activeRoutes = await _routeRepository.GetByEnvironmentAsync(createDto.Environment);
            foreach (var route in activeRoutes.Where(r => r.IsActive))
            {
                version.AddRouteConfiguration(route);
            }
        }

        await _versionRepository.AddAsync(version);
        return MapToDto(version);
    }

    public async Task<bool> PublishVersionAsync(Guid versionId, string publishedBy)
    {
        var versionToPublish = await _versionRepository.GetByIdAsync(versionId); // Renamed for clarity
        if (versionToPublish == null) return false;

        // Unpublish current active version in the same environment
        var currentActive = await _versionRepository.GetActiveConfigurationAsync(versionToPublish.Environment);
        if (currentActive != null && currentActive.Id != versionId)
        {
            currentActive.Unpublish();
            await _versionRepository.UpdateAsync(currentActive);
        }

        // Publish new version
        versionToPublish.Publish(publishedBy);
        await _versionRepository.UpdateAsync(versionToPublish);

        return true;
    }

    public async Task<bool> UnpublishVersionAsync(Guid versionId)
    {
        var versionToUnpublish = await _versionRepository.GetByIdAsync(versionId); // Renamed for clarity
        if (versionToUnpublish == null) return false;

        versionToUnpublish.Unpublish();
        await _versionRepository.UpdateAsync(versionToUnpublish);
        return true;
    }

    public async Task<bool> DeleteVersionAsync(Guid id)
    {
        var versionToDelete = await _versionRepository.GetByIdAsync(id); // Renamed for clarity
        if (versionToDelete == null) return false;

        // Don't allow deletion of active version
        if (versionToDelete.IsActive) return false;

        var deletedVersionEnvironment = versionToDelete.Environment;
        var deletedVersionNumber = versionToDelete.Version;

        await _versionRepository.DeleteAsync(id);
        return true;
    }

    public async Task<string> GenerateOcelotConfigurationAsync(Guid versionId)
    {
        var version = await _versionRepository.GetByIdAsync(versionId);
        if (version == null) return string.Empty;

        return GenerateOcelotConfiguration(version.RouteConfigurations.ToList());
    }

    public async Task<bool> ValidateConfigurationAsync(Guid versionId)
    {
        var version = await _versionRepository.GetByIdAsync(versionId);
        if (version == null) return false;

        try
        {
            // Basic validation - check if we can generate valid Ocelot configuration
            var config = GenerateOcelotConfiguration(version.RouteConfigurations.ToList());
            JsonDocument.Parse(config);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static ConfigurationVersionDto MapToDto(ConfigurationVersion version)
    {
        return new ConfigurationVersionDto
        {
            Id = version.Id,
            Version = version.Version,
            Description = version.Description,
            Environment = version.Environment,
            IsActive = version.IsActive,
            CreatedAt = version.CreatedAt,
            PublishedAt = version.PublishedAt,
            CreatedBy = version.CreatedBy,
            PublishedBy = version.PublishedBy,
            RouteConfigurations = version.RouteConfigurations.Select(r => new RouteConfigDto
            {
                Id = r.Id,
                Name = r.Name,
                DownstreamPathTemplate = r.DownstreamPathTemplate,
                UpstreamPathTemplate = r.UpstreamPathTemplate,
                UpstreamHttpMethod = r.UpstreamHttpMethod,
                UpstreamHttpMethods = r.UpstreamHttpMethods,
                DownstreamHttpMethod = r.DownstreamHttpMethod,
                DownstreamScheme = r.DownstreamScheme,
                RouteIsCaseSensitive = r.RouteIsCaseSensitive,
                DownstreamHostAndPorts = r.DownstreamHostAndPorts.Select(h => new HostAndPortDto { Host = h.Host, Port = h.Port }).ToList(),
                ServiceName = r.ServiceName,
                LoadBalancerOptions = r.LoadBalancerOptions,
                AuthenticationOptions = r.AuthenticationOptions,
                RateLimitOptions = r.RateLimitOptions,
                QoSOptions = r.QoSOptions,
                IsActive = r.IsActive,
                Environment = r.Environment,
                CreatedAt = r.CreatedAt,
                UpdatedAt = r.UpdatedAt,
                CreatedBy = r.CreatedBy,
                UpdatedBy = r.UpdatedBy
            }).ToList()
        };
    }

    private static string GenerateOcelotConfiguration(List<RouteConfig> routes)
    {
        var ocelotRoutes = routes.Select(route => new
        {
            DownstreamPathTemplate = route.DownstreamPathTemplate,
            DownstreamScheme = route.DownstreamScheme,
            DownstreamHostAndPorts = route.DownstreamHostAndPorts.Select(h => new { Host = h.Host, Port = h.Port }).ToArray(),
            UpstreamPathTemplate = route.UpstreamPathTemplate,
            UpstreamHttpMethod = route.UpstreamHttpMethods,
            RouteIsCaseSensitive = route.RouteIsCaseSensitive,
            ServiceName = route.ServiceName,
            LoadBalancerOptions = !string.IsNullOrEmpty(route.LoadBalancerOptions) ? JsonSerializer.Deserialize<object>(route.LoadBalancerOptions) : null,
            AuthenticationOptions = !string.IsNullOrEmpty(route.AuthenticationOptions) ? JsonSerializer.Deserialize<object>(route.AuthenticationOptions) : null,
            RateLimitOptions = !string.IsNullOrEmpty(route.RateLimitOptions) ? JsonSerializer.Deserialize<object>(route.RateLimitOptions) : null,
            QoSOptions = !string.IsNullOrEmpty(route.QoSOptions) ? JsonSerializer.Deserialize<object>(route.QoSOptions) : null
        }).ToList();

        var config = new
        {
            Routes = ocelotRoutes,
            GlobalConfiguration = new
            {
                BaseUrl = "https://localhost:5001"
            }
        };

        return JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
    }
} 