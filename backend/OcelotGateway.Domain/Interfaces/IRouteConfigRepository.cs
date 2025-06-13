using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using OcelotGateway.Domain.Entities;

namespace OcelotGateway.Domain.Interfaces
{
    /// <summary>
    /// Repository interface for managing RouteConfig entities
    /// </summary>
    public interface IRouteConfigRepository
    {
        Task<RouteConfig> GetByIdAsync(Guid id);
        Task<IEnumerable<RouteConfig>> GetAllAsync();
        Task<IEnumerable<RouteConfig>> GetByEnvironmentAsync(string environment);
        Task<IEnumerable<RouteConfig>> GetActiveRoutesAsync();
        Task AddAsync(RouteConfig routeConfig);
        Task UpdateAsync(RouteConfig routeConfig);
        Task DeleteAsync(Guid id);
        Task<bool> ExistsAsync(Guid id);
    }
} 