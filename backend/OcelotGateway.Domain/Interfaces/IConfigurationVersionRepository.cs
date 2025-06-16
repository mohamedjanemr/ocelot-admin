using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using OcelotGateway.Domain.Entities;

namespace OcelotGateway.Domain.Interfaces
{
    /// <summary>
    /// Repository interface for managing ConfigurationVersion entities
    /// </summary>
    public interface IConfigurationVersionRepository
    {
        Task<ConfigurationVersion> GetByIdAsync(Guid id);
        Task<ConfigurationVersion> GetByVersionAsync(string version, string environment);
        Task<ConfigurationVersion> GetActiveConfigurationAsync(string environment);
        Task<IEnumerable<ConfigurationVersion>> GetAllAsync();
        Task<IEnumerable<ConfigurationVersion>> GetByEnvironmentAsync(string environment);
        Task AddAsync(ConfigurationVersion configurationVersion);
        Task UpdateAsync(ConfigurationVersion configurationVersion);
        Task DeleteAsync(Guid id);
        Task<bool> ExistsAsync(Guid id);
    }
} 