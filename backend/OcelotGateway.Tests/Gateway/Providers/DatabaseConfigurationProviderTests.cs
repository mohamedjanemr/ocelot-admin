using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Ocelot.Configuration.File; // For FileConfiguration
using Ocelot.Configuration.Repository; // For IFileConfigurationRepository
using OcelotGateway.Domain.Entities;
using OcelotGateway.Domain.Interfaces;
using OcelotGateway.Domain.ValueObjects;
using OcelotGateway.Gateway.Providers; // Provider being tested
using OcelotGateway.Gateway.Services;   // For ConfigurationCacheService
using OcelotGateway.Infrastructure.Data;
using OcelotGateway.Infrastructure.Repositories;
using OcelotGateway.Tests.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace OcelotGateway.Tests.Gateway.Providers
{
    public class DatabaseConfigurationProviderTests : DatabaseTestBase
    {
        private readonly DatabaseConfigurationProvider _configProvider;
        private readonly ConfigurationCacheService _cacheService;
        private readonly IMemoryCache _memoryCache;
        private readonly ConfigurationVersionRepository _configVersionRepository;
        private readonly RouteConfigRepository _routeConfigRepository;
        private readonly IServiceProvider _serviceProvider;
        private const string TestEnvironment = "TestEnvDbProvider";

        public DatabaseConfigurationProviderTests()
        {
            _memoryCache = new MemoryCache(new MemoryCacheOptions());
            _configVersionRepository = new ConfigurationVersionRepository(DbContext);
            _routeConfigRepository = new RouteConfigRepository(DbContext);

            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton<IConfigurationVersionRepository>(_configVersionRepository);
            serviceCollection.AddSingleton<IRouteConfigRepository>(_routeConfigRepository);
            // Add other dependencies that DatabaseConfigurationProvider might resolve via IServiceProvider for its Set method if any.
            _serviceProvider = serviceCollection.BuildServiceProvider();

            var mockCacheLogger = new Mock<ILogger<ConfigurationCacheService>>();
            _cacheService = new ConfigurationCacheService(_memoryCache, _serviceProvider, mockCacheLogger.Object);

            var mockProviderLogger = new Mock<ILogger<DatabaseConfigurationProvider>>();
            _configProvider = new DatabaseConfigurationProvider(_serviceProvider, _cacheService, mockProviderLogger.Object, TestEnvironment);
        }

        private async Task<ConfigurationVersion> SeedActiveVersionWithRoutesAsync(string environment, int routeCount, string versionNumber = "1.0.0", bool isActive = true)
        {
            var routes = new List<RouteConfig>();
            for (int i = 0; i < routeCount; i++)
            {
                var route = new RouteConfig(
                    $"TestRoute-{environment}-{versionNumber}-{i + 1}",
                    $"/downstream/{environment.ToLower()}/route{i + 1}",
                    $"/upstream/{environment.ToLower()}/route{i + 1}",
                    "GET,POST", "http", // Ensuring UpstreamHttpMethod is a string
                    new List<HostAndPort> { new HostAndPort("localhost", 8080 + i) },
                    environment,
                    "test_seeder")
                {
                    // Ensure IsActive is true if the route itself should be considered active by Ocelot
                    // The RouteConfig entity has an IsActive property.
                };
                // Default IsActive for RouteConfig is true.
                routes.Add(route);
            }
            DbContext.RouteConfigs.AddRange(routes);
            await DbContext.SaveChangesAsync();

            var version = new ConfigurationVersion(versionNumber, $"Active version for {environment}", environment, "test_seeder");
            foreach (var route in routes)
            {
                version.AddRouteConfiguration(route);
            }
            if (isActive) version.Publish("test_seeder");

            DbContext.ConfigurationVersions.Add(version);
            await DbContext.SaveChangesAsync();
            return version;
        }

        [Fact]
        public async Task Get_NoActiveConfiguration_ReturnsEmptyConfigOrError()
        {
            // Arrange: DbContext is clean due to DatabaseTestBase for this specific TestEnvironment

            // Act
            var response = await _configProvider.Get();

            // Assert
            // Ocelot typically expects an empty FileConfiguration rather than an error if no config is found.
            // The DatabaseConfigurationProvider's Get method, when using ConfigurationCacheService,
            // will return a FileConfiguration (possibly empty) if no active version is found.
            Assert.False(response.IsError, "Response should not be an error for no active config.");
            Assert.NotNull(response.Data);
            Assert.Empty(response.Data.Routes); // Key check: no routes
            Assert.NotNull(response.Data.GlobalConfiguration); // Ocelot expects a GlobalConfiguration section.
        }

        [Fact]
        public async Task Get_ActiveConfigurationWithRoutes_ReturnsCorrectFileConfiguration()
        {
            // Arrange
            var seededVersion = await SeedActiveVersionWithRoutesAsync(TestEnvironment, 2, "1.1.0");

            // Act
            var response = await _configProvider.Get();

            // Assert
            Assert.False(response.IsError);
            Assert.NotNull(response.Data);
            var fileConfig = response.Data;

            Assert.Equal(seededVersion.RouteConfigurations.Count, fileConfig.Routes.Count);

            foreach (var seededRoute in seededVersion.RouteConfigurations.OrderBy(r => r.Name))
            {
                var fileRoute = fileConfig.Routes.FirstOrDefault(fr => fr.UpstreamPathTemplate == seededRoute.UpstreamPathTemplate);
                Assert.NotNull(fileRoute);
                Assert.Equal(seededRoute.DownstreamPathTemplate, fileRoute.DownstreamPathTemplate);
                Assert.Equal(seededRoute.DownstreamScheme, fileRoute.DownstreamScheme);
                // UpstreamHttpMethod in FileRoute is List<string>, in RouteConfig it's a comma-separated string (or should be mapped)
                // The mapping logic in ConfigurationCacheService handles splitting this.
                var expectedHttpMethods = seededRoute.UpstreamHttpMethod.Split(',').Select(m => m.Trim().ToUpperInvariant()).ToList();
                Assert.Equal(expectedHttpMethods.Count, fileRoute.UpstreamHttpMethod.Count);
                foreach(var method in expectedHttpMethods)
                {
                    Assert.Contains(method, fileRoute.UpstreamHttpMethod, StringComparer.OrdinalIgnoreCase);
                }
                Assert.Equal(seededRoute.DownstreamHostAndPorts.First().Host, fileRoute.DownstreamHostAndPorts.First().Host);
                Assert.Equal(seededRoute.DownstreamHostAndPorts.First().Port, fileRoute.DownstreamHostAndPorts.First().Port);
            }
        }

        [Fact]
        public async Task Get_RespectsEnvironmentIsolation()
        {
            // Arrange
            await SeedActiveVersionWithRoutesAsync(TestEnvironment, 1, "1.0.0"); // Provider is configured for TestEnvironment
            await SeedActiveVersionWithRoutesAsync("DifferentEnvironment", 2, "1.0.0");

            // Act
            var response = await _configProvider.Get();

            // Assert
            Assert.False(response.IsError);
            Assert.NotNull(response.Data);
            Assert.Single(response.Data.Routes); // Should only get routes for TestEnvironment
            Assert.Contains(TestEnvironment.ToLower(), response.Data.Routes.First().UpstreamPathTemplate);
        }

        [Fact]
        public async Task Set_SavesConfigurationAndMakesItActive()
        {
            // Arrange
            var fileConfigToSet = new FileConfiguration
            {
                Routes = new List<FileRoute>
                {
                    new FileRoute
                    {
                        // Name property removed in Ocelot 24.0.0
                        UpstreamPathTemplate = "/set/route1",
                        UpstreamHttpMethod = new List<string> { "GET" },
                        DownstreamPathTemplate = "/down/set1",
                        DownstreamScheme = "http",
                        DownstreamHostAndPorts = new List<FileHostAndPort> { new FileHostAndPort("sethost", 1234) }
                    }
                },
                GlobalConfiguration = new FileGlobalConfiguration { BaseUrl = "http://localhost:7000" }
            };

            // Seed an existing active version to ensure it gets deactivated
            await SeedActiveVersionWithRoutesAsync(TestEnvironment, 1, "0.9.0");


            // Act
            var setResult = await _configProvider.Set(fileConfigToSet);

            // Assert
            Assert.False(setResult.IsError);

            var activeConfigs = await _configVersionRepository.GetActiveConfigurationAsync(TestEnvironment);
            Assert.NotNull(activeConfigs);
            Assert.True(activeConfigs.IsActive);
            // The version name is auto-generated by timestamp in DatabaseConfigurationProvider.Set
            // Assert.Equal("some_expected_version_name", activeConfigs.Version); // Hard to predict timestamp-based name
            Assert.Equal($"Configuration updated at {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}", activeConfigs.Description, StringComparer.OrdinalIgnoreCase);


            Assert.Single(activeConfigs.RouteConfigurations);
            var savedRoute = activeConfigs.RouteConfigurations.First();
            // Name property comparison removed since FileRoute.Name doesn't exist in Ocelot 24.0.0
            Assert.Equal(fileConfigToSet.Routes.First().UpstreamPathTemplate, savedRoute.UpstreamPathTemplate);

            // Verify old version is deactivated
            var oldVersion = await _configVersionRepository.GetByVersionAsync("0.9.0", TestEnvironment);
            Assert.NotNull(oldVersion);
            Assert.False(oldVersion.IsActive);
        }
    }
}
