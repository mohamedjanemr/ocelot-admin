using Microsoft.AspNetCore.SignalR; // Added for SignalR
using OcelotGateway.Application.DTOs;
using OcelotGateway.Application.Interfaces;
using OcelotGateway.Domain.Entities;
using OcelotGateway.Domain.Interfaces;
using OcelotGateway.Domain.ValueObjects;
using OcelotGateway.WebApi.Hubs; // Added for ConfigurationHub

namespace OcelotGateway.Application.Services;

public class RouteConfigService : IRouteConfigService
{
    private readonly IRouteConfigRepository _routeConfigRepository;
    private readonly IHubContext<ConfigurationHub> _hubContext; // Added for SignalR

    public RouteConfigService(IRouteConfigRepository routeConfigRepository, IHubContext<ConfigurationHub> hubContext) // Added hubContext
    {
        _routeConfigRepository = routeConfigRepository;
        _hubContext = hubContext; // Added for SignalR
    }

    public async Task<IEnumerable<RouteConfigDto>> GetAllRoutesAsync()
    {
        var routes = await _routeConfigRepository.GetAllAsync();
        return routes.Select(MapToDto);
    }

    public async Task<RouteConfigDto?> GetRouteByIdAsync(Guid id)
    {
        var route = await _routeConfigRepository.GetByIdAsync(id);
        return route != null ? MapToDto(route) : null;
    }

    public async Task<IEnumerable<RouteConfigDto>> GetByEnvironmentAsync(string environment)
    {
        var routes = await _routeConfigRepository.GetByEnvironmentAsync(environment);
        return routes.Select(MapToDto);
    }

    public async Task<IEnumerable<RouteConfigDto>> GetActiveRoutesAsync()
    {
        var routes = await _routeConfigRepository.GetActiveRoutesAsync();
        return routes.Select(MapToDto);
    }

    public async Task<RouteConfigDto> CreateRouteAsync(CreateRouteConfigDto createDto, string createdBy)
    {
        var hostAndPorts = createDto.DownstreamHostAndPorts.Select(h => new HostAndPort(h.Host, h.Port)).ToList();
        
        var route = new RouteConfig(
            createDto.Name,
            createDto.DownstreamPathTemplate,
            createDto.UpstreamPathTemplate,
            createDto.UpstreamHttpMethod,
            createDto.DownstreamScheme,
            hostAndPorts,
            createDto.Environment,
            createdBy);

        // Set optional properties
        if (!string.IsNullOrEmpty(createDto.ServiceName))
            route.SetServiceName(createDto.ServiceName);
        if (!string.IsNullOrEmpty(createDto.LoadBalancerOptions))
            route.SetLoadBalancerOptions(createDto.LoadBalancerOptions);
        if (!string.IsNullOrEmpty(createDto.AuthenticationOptions))
            route.SetAuthenticationOptions(createDto.AuthenticationOptions);
        if (!string.IsNullOrEmpty(createDto.RateLimitOptions))
            route.SetRateLimitOptions(createDto.RateLimitOptions);
        if (!string.IsNullOrEmpty(createDto.QoSOptions))
            route.SetQoSOptions(createDto.QoSOptions);

        await _routeConfigRepository.AddAsync(route);
        // Send SignalR notification
        await _hubContext.Clients.All.SendAsync("ConfigurationChanged", new { Environment = route.Environment, Type = "RouteCreated", RouteName = route.Name, Timestamp = DateTime.UtcNow });
        return MapToDto(route);
    }

    public async Task<RouteConfigDto?> UpdateRouteAsync(Guid id, UpdateRouteConfigDto updateDto, string updatedBy)
    {
        var routeToUpdate = await _routeConfigRepository.GetByIdAsync(id); // Renamed for clarity
        if (routeToUpdate == null) return null;

        var hostAndPorts = updateDto.DownstreamHostAndPorts.Select(h => new HostAndPort(h.Host, h.Port)).ToList();

        route.Update(
            updateDto.Name,
            updateDto.DownstreamPathTemplate,
            updateDto.UpstreamPathTemplate,
            updateDto.UpstreamHttpMethod,
            updateDto.DownstreamScheme,
            hostAndPorts,
            updatedBy);

        // Update optional properties
        if (!string.IsNullOrEmpty(updateDto.ServiceName))
            route.SetServiceName(updateDto.ServiceName);
        if (!string.IsNullOrEmpty(updateDto.LoadBalancerOptions))
            route.SetLoadBalancerOptions(updateDto.LoadBalancerOptions);
        if (!string.IsNullOrEmpty(updateDto.AuthenticationOptions))
            route.SetAuthenticationOptions(updateDto.AuthenticationOptions);
        if (!string.IsNullOrEmpty(updateDto.RateLimitOptions))
            route.SetRateLimitOptions(updateDto.RateLimitOptions);
        if (!string.IsNullOrEmpty(updateDto.QoSOptions))
            routeToUpdate.SetQoSOptions(updateDto.QoSOptions);

        await _routeConfigRepository.UpdateAsync(routeToUpdate);
        // Send SignalR notification
        await _hubContext.Clients.All.SendAsync("ConfigurationChanged", new { Environment = routeToUpdate.Environment, Type = "RouteUpdated", RouteName = routeToUpdate.Name, Timestamp = DateTime.UtcNow });
        return MapToDto(routeToUpdate);
    }

    public async Task<bool> DeleteRouteAsync(Guid id)
    {
        var routeToDelete = await _routeConfigRepository.GetByIdAsync(id);
        if (routeToDelete == null) return false;

        var deletedRouteEnvironment = routeToDelete.Environment;
        var deletedRouteName = routeToDelete.Name; // Capture name for notification

        await _routeConfigRepository.DeleteAsync(id);
        // Send SignalR notification
        await _hubContext.Clients.All.SendAsync("ConfigurationChanged", new { Environment = deletedRouteEnvironment, Type = "RouteDeleted", RouteName = deletedRouteName, Timestamp = DateTime.UtcNow });
        return true;
    }

    public async Task<bool> ToggleRouteStatusAsync(Guid id, bool isActive)
    {
        var routeToToggle = await _routeConfigRepository.GetByIdAsync(id); // Renamed for clarity
        if (routeToToggle == null) return false;

        if (isActive)
            routeToToggle.Activate();
        else
            routeToToggle.Deactivate();

        await _routeConfigRepository.UpdateAsync(routeToToggle);
        // Send SignalR notification for status change
        await _hubContext.Clients.All.SendAsync("ConfigurationChanged", new { Environment = routeToToggle.Environment, Type = "RouteStatusChanged", RouteName = routeToToggle.Name, IsActive = isActive, Timestamp = DateTime.UtcNow });
        return true;
    }

    public async Task<bool> ValidateRouteConfigAsync(CreateRouteConfigDto routeConfig)
    {
        // Basic validation logic
        if (string.IsNullOrWhiteSpace(routeConfig.Name) ||
            string.IsNullOrWhiteSpace(routeConfig.DownstreamPathTemplate) ||
            string.IsNullOrWhiteSpace(routeConfig.UpstreamPathTemplate) ||
            string.IsNullOrWhiteSpace(routeConfig.UpstreamHttpMethod) ||
            string.IsNullOrWhiteSpace(routeConfig.DownstreamScheme) ||
            !routeConfig.DownstreamHostAndPorts.Any())
        {
            return false;
        }

        // Check for duplicate upstream path template in the same environment
        var existingRoutes = await _routeConfigRepository.GetByEnvironmentAsync(routeConfig.Environment);
        return !existingRoutes.Any(r => r.UpstreamPathTemplate == routeConfig.UpstreamPathTemplate && r.IsActive);
    }

    private static RouteConfigDto MapToDto(RouteConfig route)
    {
        return new RouteConfigDto
        {
            Id = route.Id,
            Name = route.Name,
            DownstreamPathTemplate = route.DownstreamPathTemplate,
            UpstreamPathTemplate = route.UpstreamPathTemplate,
            UpstreamHttpMethod = route.UpstreamHttpMethod,
            UpstreamHttpMethods = route.UpstreamHttpMethods,
            DownstreamHttpMethod = route.DownstreamHttpMethod,
            DownstreamScheme = route.DownstreamScheme,
            RouteIsCaseSensitive = route.RouteIsCaseSensitive,
            DownstreamHostAndPorts = route.DownstreamHostAndPorts.Select(h => new HostAndPortDto { Host = h.Host, Port = h.Port }).ToList(),
            ServiceName = route.ServiceName,
            LoadBalancerOptions = route.LoadBalancerOptions,
            AuthenticationOptions = route.AuthenticationOptions,
            RateLimitOptions = route.RateLimitOptions,
            QoSOptions = route.QoSOptions,
            IsActive = route.IsActive,
            Environment = route.Environment,
            CreatedAt = route.CreatedAt,
            UpdatedAt = route.UpdatedAt,
            CreatedBy = route.CreatedBy,
            UpdatedBy = route.UpdatedBy
        };
    }
} 