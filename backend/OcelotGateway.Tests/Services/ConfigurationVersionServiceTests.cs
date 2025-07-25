using Microsoft.Extensions.Logging;
using Moq;
using OcelotGateway.Application.DTOs;
using OcelotGateway.Application.Services;
using OcelotGateway.Domain.Entities;
using OcelotGateway.Domain.ValueObjects;
using OcelotGateway.Infrastructure.Data;
using OcelotGateway.Infrastructure.Repositories;
using OcelotGateway.Tests.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace OcelotGateway.Tests.Services
{
    public class ConfigurationVersionServiceTests : DatabaseTestBase
    {
        private readonly ConfigurationVersionService _configVersionService;
        private readonly RouteConfigRepository _routeConfigRepository; // Needed for seeding routes for versions

        public ConfigurationVersionServiceTests()
        {
            var configVersionRepository = new ConfigurationVersionRepository(DbContext);
            _routeConfigRepository = new RouteConfigRepository(DbContext); // Instantiated for use in test setups

            // ConfigurationVersionService constructor: (IConfigurationVersionRepository, IRouteConfigRepository)
            _configVersionService = new ConfigurationVersionService(configVersionRepository, _routeConfigRepository);
        }

        private RouteConfig SeedRoute(string name = "TestRoute", string env = "Development", string createdBy = "test_user")
        {
            var route = new RouteConfig(
                name, $"/downstream{name}", $"/upstream{name}", "GET", "http",
                new List<HostAndPort> { new HostAndPort("localhost", 8000) }, env, createdBy);
            DbContext.RouteConfigs.Add(route);
            DbContext.SaveChanges();
            return route;
        }

        private CreateConfigurationVersionDto CreateSampleVersionDto(string version = "1.0.0", string env = "Development", List<Guid>? routeIds = null)
        {
            return new CreateConfigurationVersionDto
            {
                Version = version,
                Description = $"Test version {version}",
                Environment = env,
                RouteIds = routeIds ?? new List<Guid>()
            };
        }

        private ConfigurationVersion SeedVersionDirectly(string versionNumber = "1.0.0", string env = "Development", bool isActive = false, string createdBy = "seed_user", List<RouteConfig>? routes = null)
        {
            var version = new ConfigurationVersion(versionNumber, $"Desc for {versionNumber}", env, createdBy);
            if (routes != null)
            {
                foreach(var route in routes)
                {
                    version.AddRouteConfiguration(route);
                }
            }
            if (isActive) version.Publish(createdBy);

            DbContext.ConfigurationVersions.Add(version);
            DbContext.SaveChanges();
            return version;
        }


        [Fact]
        public async Task CreateVersionAsync_ShouldSaveToDatabaseAndNotifyHub()
        {
            // Arrange
            var route1 = SeedRoute("Route1ForCreate");
            var dto = CreateSampleVersionDto("1.0.1", "Development", new List<Guid> { route1.Id });
            var createdBy = "test_creator";

            // Act
            var result = await _configVersionService.CreateVersionAsync(dto, createdBy);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(dto.Version, result.Version);
            var versionInDb = await DbContext.ConfigurationVersions.FindAsync(result.Id);
            Assert.NotNull(versionInDb);
            Assert.Equal(dto.Version, versionInDb.Version);
            Assert.Equal(createdBy, versionInDb.CreatedBy);
            Assert.Contains(versionInDb.RouteConfigurations, rc => rc.Id == route1.Id);

            // SignalR verification removed since service no longer uses SignalR
        }

        [Fact]
        public async Task PublishVersionAsync_ShouldUpdateActiveStatusAndNotifyHub()
        {
            // Arrange
            var env = "Staging";
            var routeA = SeedRoute("RouteA", env);
            var initiallyActiveVersion = SeedVersionDirectly("0.9.0", env, isActive: true, createdBy: "system", routes: new List<RouteConfig>{ routeA });
            var versionToPublish = SeedVersionDirectly("1.0.0", env, isActive: false, createdBy: "user1", routes: new List<RouteConfig>{ routeA });
            var publisher = "test_publisher";


            // Act
            var publishResult = await _configVersionService.PublishVersionAsync(versionToPublish.Id, publisher);

            // Assert
            Assert.True(publishResult);

            var publishedVersionInDb = await DbContext.ConfigurationVersions.FindAsync(versionToPublish.Id);
            Assert.True(publishedVersionInDb.IsActive);
            Assert.Equal(publisher, publishedVersionInDb.PublishedBy);

            var oldActiveVersionInDb = await DbContext.ConfigurationVersions.FindAsync(initiallyActiveVersion.Id);
            Assert.False(oldActiveVersionInDb.IsActive); // Should be unpublished

            // SignalR verification removed since service no longer uses SignalR
        }

        [Fact]
        public async Task UnpublishVersionAsync_ShouldDeactivateAndNotifyHub()
        {
            // Arrange
            var env = "Production";
            var versionToUnpublish = SeedVersionDirectly("2.0.0", env, isActive: true, createdBy: "admin");

            // Act
            var unpublishResult = await _configVersionService.UnpublishVersionAsync(versionToUnpublish.Id);

            // Assert
            Assert.True(unpublishResult);
            var versionInDb = await DbContext.ConfigurationVersions.FindAsync(versionToUnpublish.Id);
            Assert.False(versionInDb.IsActive);

            // SignalR verification removed since service no longer uses SignalR
        }

        [Fact]
        public async Task DeleteVersionAsync_ShouldRemoveFromDatabaseAndNotifyHub_IfNotActive()
        {
            // Arrange
            var versionToDelete = SeedVersionDirectly("0.5.0-beta", "TestEnv", isActive: false); // Must be inactive

            // Act
            var deleteResult = await _configVersionService.DeleteVersionAsync(versionToDelete.Id);

            // Assert
            Assert.True(deleteResult);
            var versionInDb = await DbContext.ConfigurationVersions.FindAsync(versionToDelete.Id);
            Assert.Null(versionInDb);

            // SignalR verification removed since service no longer uses SignalR
        }

        [Fact]
        public async Task DeleteVersionAsync_ShouldNotDeleteActiveVersion()
        {
            // Arrange
            var activeVersion = SeedVersionDirectly("1.0.0-active", "Prod", isActive: true);

            // Act
            var deleteResult = await _configVersionService.DeleteVersionAsync(activeVersion.Id);

            // Assert
            Assert.False(deleteResult); // Should not delete active version
            var versionInDb = await DbContext.ConfigurationVersions.FindAsync(activeVersion.Id);
            Assert.NotNull(versionInDb); // Still exists
            // SignalR verification removed since service no longer uses SignalR
        }


        [Fact]
        public async Task GetVersionByIdAsync_ShouldReturnCorrectVersion()
        {
            // Arrange
            var seededVersion = SeedVersionDirectly("3.0.0", "Dev");

            // Act
            var result = await _configVersionService.GetVersionByIdAsync(seededVersion.Id);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(seededVersion.Version, result.Version);
            Assert.Equal(seededVersion.Environment, result.Environment);
        }

        [Fact]
        public async Task GetActiveConfigurationAsync_ShouldReturnActiveVersionForEnvironment()
        {
            // Arrange
            var env = "UAT";
            SeedVersionDirectly("1.0", env, isActive: false); // Inactive
            var activeSeededVersion = SeedVersionDirectly("1.1", env, isActive: true); // Active
            SeedVersionDirectly("1.2", env, isActive: false); // Inactive

            // Act
            var result = await _configVersionService.GetActiveConfigurationAsync(env);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(activeSeededVersion.Id, result.Id);
            Assert.True(result.IsActive);
        }
    }
}
