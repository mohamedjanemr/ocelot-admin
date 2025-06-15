using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration; // For IConfigurationBuilder
using Microsoft.Extensions.DependencyInjection;
using OcelotGateway.Infrastructure.Data; // For ApplicationDbContext
using System;
using System.Collections.Generic; // For Dictionary
using System.Data.Common;     // For DbConnection
using System.Linq;

// Assuming OcelotGateway.Gateway.Program is accessible for WebApplicationFactory.

namespace OcelotGateway.Tests.Factories
{
    public class GatewayWebApplicationFactory : WebApplicationFactory<OcelotGateway.Gateway.Program>
    {
        private readonly DbConnection? _dbConnection;
        private readonly string? _signalRHubUrl;

        // Default constructor for tests not needing shared DB or specific SignalR URL
        public GatewayWebApplicationFactory() : this(null, null)
        {
        }

        // Constructor for tests requiring shared DB and/or specific SignalR Hub URL
        public GatewayWebApplicationFactory(DbConnection? dbConnection, string? signalRHubUrl)
        {
            _dbConnection = dbConnection;
            _signalRHubUrl = signalRHubUrl;
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            // Configure AppConfiguration to set the SignalR Hub URL
            if (!string.IsNullOrEmpty(_signalRHubUrl))
            {
                builder.ConfigureAppConfiguration((context, configBuilder) =>
                {
                    configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        { "SignalR:HubUrl", _signalRHubUrl } // Key used by SignalRService
                    });
                });
            }

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
                        options.UseSqlite(_dbConnection);
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

                // Ensure the database is created for every test run using this factory.
                var sp = services.BuildServiceProvider();
                using (var scope = sp.CreateScope())
                {
                    var scopedServices = scope.ServiceProvider;
                    var db = scopedServices.GetRequiredService<ApplicationDbContext>();
                    db.Database.EnsureCreated();
                    // SeedDataForGateway(db); // Optional: Seed data specific to Gateway tests
                }
            });

            builder.UseEnvironment("Test"); // Use a specific environment for tests
        }

        // Optional: Method to seed data specific to Gateway tests
        // protected virtual void SeedDataForGateway(ApplicationDbContext context)
        // {
        //     // Add any common seed data for Gateway
        // }
    }
}
