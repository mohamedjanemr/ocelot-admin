using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using OcelotGateway.Domain.Entities;
using OcelotGateway.Domain.Interfaces;
using OcelotGateway.Domain.ValueObjects;
using OcelotGateway.Gateway.Services; // Service being tested
using OcelotGateway.Infrastructure.Data;
using OcelotGateway.Infrastructure.Repositories;
using OcelotGateway.Tests.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace OcelotGateway.Tests.Gateway.Services
{
    public class ConfigurationCacheServiceTests : DatabaseTestBase
    {
        private readonly ConfigurationCacheService _cacheService;
        private readonly IMemoryCache _memoryCache;
        private readonly ConfigurationVersionRepository _configVersionRepository; // To seed data and verify DB interactions

        public ConfigurationCacheServiceTests()
        {
            _memoryCache = new MemoryCache(new MemoryCacheOptions());
            _configVersionRepository = new ConfigurationVersionRepository(DbContext); // DbContext from DatabaseTestBase

            // Setup IServiceProvider to resolve IConfigurationVersionRepository
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton<IConfigurationVersionRepository>(_configVersionRepository);
            var serviceProvider = serviceCollection.BuildServiceProvider();

            var mockLogger = new Mock<ILogger<ConfigurationCacheService>>();

            _cacheService = new ConfigurationCacheService(_memoryCache, serviceProvider, mockLogger.Object);
        }

        private async Task<ConfigurationVersion> SeedActiveVersionWithRoutesAsync(string environment, int routeCount, string versionNumber = "1.0.0")
        {
            var routes = new List<RouteConfig>();
            for (int i = 0; i < routeCount; i++)
            {
                var route = new RouteConfig(
                    $"TestRoute{i + 1}",
                    $"/downstream/{environment.ToLower()}/route{i + 1}",
                    $"/upstream/{environment.ToLower()}/route{i + 1}",
                    "GET", "http",
                    new List<HostAndPort> { new HostAndPort("localhost", 8080 + i) },
                    environment,
                    "test_seeder");
                routes.Add(route);
            }
            // Save routes first if they are independent entities in the DB before associating with a version
            DbContext.RouteConfigs.AddRange(routes);
            await DbContext.SaveChangesAsync();


            var version = new ConfigurationVersion(versionNumber, $"Active version for {environment}", environment, "test_seeder");
            foreach (var route in routes)
            {
                version.AddRouteConfiguration(route);
            }
            version.Publish("test_seeder"); // Make it active

            DbContext.ConfigurationVersions.Add(version);
            await DbContext.SaveChangesAsync();
            return version;
        }

        [Fact]
        public async Task GetConfigurationAsync_CacheMiss_LoadsFromDbAndCaches()
        {
            // Arrange
            var environment = "CacheMissEnv";
            var seededVersion = await SeedActiveVersionWithRoutesAsync(environment, 2, "1.0.0");
            // Ensure cache is empty for this environment key
            _memoryCache.Remove($"{environment}_ocelot_config");


            // Act
            var fileConfig = await _cacheService.GetConfigurationAsync(environment);

            // Assert
            Assert.NotNull(fileConfig);
            Assert.Equal(seededVersion.RouteConfigurations.Count, fileConfig.Routes.Count);
            Assert.Equal(seededVersion.RouteConfigurations.First().Name, fileConfig.Routes.First().Name);

            // Check if it's in cache now by trying to get it again
            // A true "Same" check might not work if a new object is constructed from cache.
            // Instead, we can verify that the data is correct and, if we had mocked the repo, verify it wasn't called again.
            // Here, we'll just retrieve again and assume if data is right, it worked.
            var cachedConfig = await _cacheService.GetConfigurationAsync(environment);
            Assert.NotNull(cachedConfig);
            Assert.Equal(fileConfig.Routes.Count, cachedConfig.Routes.Count);
            Assert.Equal(fileConfig.Routes.First().Name, cachedConfig.Routes.First().Name);

            // To be more certain it's cached, we could try to retrieve the item from IMemoryCache directly.
            var cacheKey = $"{environment}_ocelot_config";
            var isCached = _memoryCache.TryGetValue(cacheKey, out Ocelot.Configuration.File.FileConfiguration _);
            Assert.True(isCached, "Configuration should be in cache after first retrieval.");
        }

        [Fact]
        public async Task GetConfigurationAsync_CacheHit_ReturnsCachedData()
        {
            // Arrange
            var environment = "CacheHitEnv";
            var initialVersion = await SeedActiveVersionWithRoutesAsync(environment, 1, "1.0.0");

            // Act: First call to populate cache
            var initialConfig = await _cacheService.GetConfigurationAsync(environment);
            Assert.NotNull(initialConfig);
            Assert.Single(initialConfig.Routes);
            Assert.Equal(initialVersion.RouteConfigurations.First().Name, initialConfig.Routes.First().Name);

            // Modify data in DB *after* caching.
            // Add a new route to the existing version, or create a new active version.
            // For simplicity, let's add a new route to the DB and make a new version active.
            await SeedActiveVersionWithRoutesAsync(environment, 2, "1.0.1"); // This new version 1.0.1 is now active

            // Act: Second call, should hit cache
            var cachedConfig = await _cacheService.GetConfigurationAsync(environment);

            // Assert
            Assert.NotNull(cachedConfig);
            // Crucially, it should still have the count from the *initial* caching, not the updated DB.
            Assert.Single(cachedConfig.Routes);
            Assert.Equal(initialVersion.RouteConfigurations.First().Name, cachedConfig.Routes.First().Name);
        }

        [Fact]
        public async Task InvalidateCache_ClearsCacheForKey_AndNextGetLoadsFromDb()
        {
            // Arrange
            var environment = "InvalidateEnv";
            var initialVersion = await SeedActiveVersionWithRoutesAsync(environment, 1, "1.0.0");

            // Populate cache
            var configBeforeInvalidation = await _cacheService.GetConfigurationAsync(environment);
            Assert.NotNull(configBeforeInvalidation);
            Assert.Single(configBeforeInvalidation.Routes);

            // Act: Invalidate cache
            _cacheService.InvalidateCache(environment);

            // Modify data in DB - e.g., a new version is now active with different routes
            await SeedActiveVersionWithRoutesAsync(environment, 2, "1.0.1"); // Version 1.0.1 with 2 routes now active

            // Act: Get configuration again after invalidation
            var configAfterInvalidation = await _cacheService.GetConfigurationAsync(environment);

            // Assert
            Assert.NotNull(configAfterInvalidation);
            // Should reflect the new state from the DB (2 routes from version 1.0.1)
            Assert.Equal(2, configAfterInvalidation.Routes.Count);
        }

        [Fact]
        public async Task GetConfigurationAsync_NoActiveVersion_ReturnsNullOrEmptyConfig()
        {
            // Arrange
            var environment = "NoActiveEnv";
            // Ensure no active version for this environment (DatabaseTestBase ensures clean DB initially)

            // Act
            var fileConfig = await _cacheService.GetConfigurationAsync(environment);

            // Assert
            // Depending on implementation, it might return null or an empty FileConfiguration
            // Based on Ocelot's typical behavior, an empty config (with empty Routes list) is common.
            Assert.NotNull(fileConfig);
            Assert.Empty(fileConfig.Routes);
            Assert.NotNull(fileConfig.GlobalConfiguration); // Ocelot usually provides a default global config.
        }
    }
}
