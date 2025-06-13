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
    public class RouteConfigRepository : IRouteConfigRepository
    {
        private readonly ApplicationDbContext _context;

        public RouteConfigRepository(ApplicationDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public async Task<RouteConfig> GetByIdAsync(Guid id)
        {
            return await _context.RouteConfigs.FindAsync(id);
        }

        public async Task<IEnumerable<RouteConfig>> GetAllAsync()
        {
            return await _context.RouteConfigs.ToListAsync();
        }

        public async Task<IEnumerable<RouteConfig>> GetByEnvironmentAsync(string environment)
        {
            return await _context.RouteConfigs
                .Where(r => r.Environment == environment)
                .ToListAsync();
        }

        public async Task<IEnumerable<RouteConfig>> GetActiveRoutesAsync()
        {
            return await _context.RouteConfigs
                .Where(r => r.IsActive)
                .ToListAsync();
        }

        public async Task AddAsync(RouteConfig routeConfig)
        {
            if (routeConfig == null)
                throw new ArgumentNullException(nameof(routeConfig));

            await _context.RouteConfigs.AddAsync(routeConfig);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(RouteConfig routeConfig)
        {
            if (routeConfig == null)
                throw new ArgumentNullException(nameof(routeConfig));

            _context.RouteConfigs.Update(routeConfig);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(Guid id)
        {
            var routeConfig = await _context.RouteConfigs.FindAsync(id);
            if (routeConfig != null)
            {
                _context.RouteConfigs.Remove(routeConfig);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<bool> ExistsAsync(Guid id)
        {
            return await _context.RouteConfigs.AnyAsync(r => r.Id == id);
        }
    }
} 