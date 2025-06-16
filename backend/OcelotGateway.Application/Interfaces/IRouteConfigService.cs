using OcelotGateway.Application.DTOs;

namespace OcelotGateway.Application.Interfaces;

public interface IRouteConfigService
{
    Task<IEnumerable<RouteConfigDto>> GetAllRoutesAsync();
    Task<RouteConfigDto?> GetRouteByIdAsync(Guid id);
    Task<IEnumerable<RouteConfigDto>> GetByEnvironmentAsync(string environment);
    Task<IEnumerable<RouteConfigDto>> GetActiveRoutesAsync();
    Task<RouteConfigDto> CreateRouteAsync(CreateRouteConfigDto createDto, string createdBy);
    Task<RouteConfigDto?> UpdateRouteAsync(Guid id, UpdateRouteConfigDto updateDto, string updatedBy);
    Task<bool> DeleteRouteAsync(Guid id);
    Task<bool> ToggleRouteStatusAsync(Guid id, bool isActive);
    Task<bool> ValidateRouteConfigAsync(CreateRouteConfigDto routeConfig);
} 