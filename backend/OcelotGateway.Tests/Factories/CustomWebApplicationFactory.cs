using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OcelotGateway.Infrastructure.Data;
using System;
using System.Data.Common; // For DbConnection
using System.Linq;

// Ensure OcelotGateway.WebApi.Program is accessible. May need to adjust if Program is not public or in a different namespace.
// For .NET 6+ minimal APIs, Program is often a top-level statements file, making WebApplicationFactory<Program> work.
// If WebApi's Program class is explicitly defined and public, it should be fine.

namespace OcelotGateway.Tests.Factories
{
    public class CustomWebApplicationFactory<TProgram> : WebApplicationFactory<TProgram> where TProgram : class
    {
        private readonly DbConnection? _dbConnection;

        // Default constructor: creates a unique in-memory database
        public CustomWebApplicationFactory() : this(null)
        {
        }

        // Constructor for tests that manage their own DbConnection (e.g., for shared SQLite in-memory databases)
        public CustomWebApplicationFactory(DbConnection? dbConnection)
        {
            _dbConnection = dbConnection;
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                // Remove the app's ApplicationDbContext registration.
                var dbContextDescriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));

                if (dbContextDescriptor != null)
                {
                    services.Remove(dbContextDescriptor);
                }

                if (_dbConnection != null)
                {
                    // Use the shared DbConnection (typically SqliteConnection for shared in-memory)
                    services.AddDbContext<ApplicationDbContext>(options =>
                    {
                        options.UseSqlite(_dbConnection); // Use provided connection
                    });
                }
                else
                {
                    // Fallback to unique in-memory database if no shared connection is provided
                    var dbName = Guid.NewGuid().ToString();
                    services.AddDbContext<ApplicationDbContext>(options =>
                    {
                        options.UseInMemoryDatabase(dbName);
                    });
                }

                // Optional: You can further customize services here.
                // Example for mocking SignalR if needed for some tests:
                // var hubContextDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IHubContext<ConfigurationHub>));
                // if (hubContextDescriptor != null) services.Remove(hubContextDescriptor);
                // var mockHubContext = new Mock<IHubContext<ConfigurationHub>>();
                // /* setup mockHubContext.Clients.All.SendAsync as needed */
                // services.AddSingleton(mockHubContext.Object);

                // Ensure the database is created for every test run using this factory.
                // This is important for both shared (SQLite) and unique (InMemory) DB scenarios.
                var sp = services.BuildServiceProvider();
                using (var scope = sp.CreateScope())
                {
                    var scopedServices = scope.ServiceProvider;
                    var db = scopedServices.GetRequiredService<ApplicationDbContext>();
                    db.Database.EnsureCreated();
                    // SeedData(db); // Optional: Seed data if needed for all tests using this factory
                }
            });

            builder.UseEnvironment("Test"); // Use a specific environment for tests if needed
        }

        // Optional: Method to seed data into the in-memory database
        // protected virtual void SeedData(ApplicationDbContext context)
        // {
        //     // Add any common seed data
        // }
    }
}
