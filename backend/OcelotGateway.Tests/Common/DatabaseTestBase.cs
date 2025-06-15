using Microsoft.EntityFrameworkCore;
using OcelotGateway.Infrastructure.Data;
using System;

namespace OcelotGateway.Tests.Common
{
    public abstract class DatabaseTestBase : IDisposable
    {
        protected readonly ApplicationDbContext DbContext;
        private readonly DbContextOptions<ApplicationDbContext> _options;

        protected DatabaseTestBase()
        {
            _options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()) // Unique name per test class instance
                .Options;
            DbContext = new ApplicationDbContext(_options);
            DbContext.Database.EnsureCreated();
            SeedData(DbContext);
        }

        protected virtual void SeedData(ApplicationDbContext context)
        {
            // This method can be overridden by derived test classes to add specific seed data.
            // For example:
            // context.RouteConfigs.Add(new RouteConfig(...));
            // context.SaveChanges();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                DbContext.Database.EnsureDeleted();
                DbContext.Dispose();
            }
        }
    }
}
