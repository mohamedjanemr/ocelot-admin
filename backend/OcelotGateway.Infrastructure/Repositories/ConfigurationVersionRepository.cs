using Microsoft.EntityFrameworkCore;
using OcelotGateway.Domain.Entities;
using OcelotGateway.Domain.Interfaces;
using OcelotGateway.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OcelotGateway.Infrastructure.Repositories
{
    public class ConfigurationVersionRepository : IConfigurationVersionRepository
    {
        private readonly ApplicationDbContext _context;

        public ConfigurationVersionRepository(ApplicationDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<ConfigurationVersion> GetByIdAsync(Guid id)
        {
            return await _context.ConfigurationVersions
                .Include(c => c.RouteConfigurations)
                .FirstOrDefaultAsync(c => c.Id == id);
        }

        public async Task<ConfigurationVersion> GetByVersionAsync(string version, string environment)
        {
            return await _context.ConfigurationVersions
                .Include(c => c.RouteConfigurations)
                .FirstOrDefaultAsync(c => c.Version == version && c.Environment == environment);
        }

        public async Task<ConfigurationVersion> GetActiveConfigurationAsync(string environment)
        {
            return await _context.ConfigurationVersions
                .Include(c => c.RouteConfigurations)
                .FirstOrDefaultAsync(c => c.IsActive && c.Environment == environment);
        }

        public async Task<IEnumerable<ConfigurationVersion>> GetAllAsync()
        {
            return await _context.ConfigurationVersions
                .Include(c => c.RouteConfigurations)
                .ToListAsync();
        }

        public async Task<IEnumerable<ConfigurationVersion>> GetByEnvironmentAsync(string environment)
        {
            return await _context.ConfigurationVersions
                .Include(c => c.RouteConfigurations)
                .Where(c => c.Environment == environment)
                .ToListAsync();
        }

        public async Task AddAsync(ConfigurationVersion configurationVersion)
        {
            if (configurationVersion == null)
                throw new ArgumentNullException(nameof(configurationVersion));

            await _context.ConfigurationVersions.AddAsync(configurationVersion);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(ConfigurationVersion configurationVersion)
        {
            if (configurationVersion == null)
                throw new ArgumentNullException(nameof(configurationVersion));

            _context.ConfigurationVersions.Update(configurationVersion);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(Guid id)
        {
            var configurationVersion = await _context.ConfigurationVersions.FindAsync(id);
            if (configurationVersion != null)
            {
                _context.ConfigurationVersions.Remove(configurationVersion);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<bool> ExistsAsync(Guid id)
        {
            return await _context.ConfigurationVersions.AnyAsync(c => c.Id == id);
        }
    }
} 