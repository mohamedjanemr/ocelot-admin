using Microsoft.Extensions.Logging;
using Moq;
using OcelotGateway.Application.DTOs;
using OcelotGateway.Application.Interfaces;
using OcelotGateway.Application.Services;
using OcelotGateway.Domain.Entities;
using OcelotGateway.Domain.ValueObjects;
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
    public class RouteConfigServiceTests : DatabaseTestBase // Inherits from DatabaseTestBase for DbContext
    {
        private readonly RouteConfigService _routeConfigService;

        public RouteConfigServiceTests()
        {

            // Setup Repository
            var routeRepository = new RouteConfigRepository(DbContext); // DbContext comes from DatabaseTestBase

            // Instantiate the Service (no longer takes SignalR hub context)
            _routeConfigService = new RouteConfigService(routeRepository);
        }

        private CreateRouteConfigDto CreateSampleRouteDto(string name = "TestRoute", string env = "Development", bool isActive = true)
        {
            // The CreateRouteConfigDto does not have IsActive. It's managed by ToggleRouteStatusAsync or default entity state.
            return new CreateRouteConfigDto
            {
                Name = name,
                DownstreamPathTemplate = "/downstream/{everything}",
                UpstreamPathTemplate = "/upstream/{everything}",
                UpstreamHttpMethod = "GET,POST", // This should be a string if UpstreamHttpMethods is a list in the entity
                DownstreamScheme = "http",
                DownstreamHostAndPorts = new List<HostAndPortDto> { new HostAndPortDto { Host = "localhost", Port = 8080 } },
                Environment = env,
                ServiceName = "TestService"
            };
        }

        private RouteConfig SeedRouteDirectly(string name = "SeededRoute", string env = "Development", bool isActive = true, string createdBy = "seed_user")
        {
            var route = new RouteConfig(
                name,
                $"/downstream/{name.ToLower()}",
                $"/upstream/{name.ToLower()}",
                "GET",
                "http",
                new List<HostAndPort> { new HostAndPort("localhost", 9090) },
                env,
                createdBy
            );
            if (!isActive) route.Deactivate();
            DbContext.RouteConfigs.Add(route);
            DbContext.SaveChanges();
            return route;
        }


        [Fact]
        public async Task CreateRouteAsync_ShldSaveToDatabaseAndNotifyHub()
        {
            // Arrange
            var dto = CreateSampleRouteDto();
            var createdBy = "test_user";

            // Act
            var result = await _routeConfigService.CreateRouteAsync(dto, createdBy);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(dto.Name, result.Name);

            var routeInDb = await DbContext.RouteConfigs.FindAsync(result.Id);
            Assert.NotNull(routeInDb);
            Assert.Equal(dto.Name, routeInDb.Name);
            Assert.Equal(createdBy, routeInDb.CreatedBy);
            Assert.Equal(dto.Environment, routeInDb.Environment);
            Assert.True(routeInDb.IsActive); // Default should be active

            // SignalR verification removed since service no longer uses SignalR
        }

        [Fact]
        public async Task UpdateRouteAsync_ShouldUpdateDatabaseAndNotifyHub()
        {
            // Arrange
            var initialEntity = SeedRouteDirectly("InitialRouteForUpdate");

            var updateDto = new UpdateRouteConfigDto
            {
                Name = "UpdatedRouteName",
                DownstreamPathTemplate = "/newdownstream/{everything}",
                UpstreamPathTemplate = initialEntity.UpstreamPathTemplate, // Usually upstream path is key, not changed like this
                UpstreamHttpMethod = "PUT", // This should be a string if UpstreamHttpMethods is a list in the entity
                DownstreamScheme = "https",
                DownstreamHostAndPorts = new List<HostAndPortDto> { new HostAndPortDto { Host = "remotehost", Port = 443 } },
                ServiceName = "UpdatedService"
            };
            var updatedBy = "test_user_update";

            // Act
            var result = await _routeConfigService.UpdateRouteAsync(initialEntity.Id, updateDto, updatedBy);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(updateDto.Name, result.Name);
            Assert.Equal(updateDto.DownstreamPathTemplate, result.DownstreamPathTemplate);

            var routeInDb = await DbContext.RouteConfigs.FindAsync(initialEntity.Id);
            Assert.NotNull(routeInDb);
            Assert.Equal(updateDto.Name, routeInDb.Name);
            Assert.Equal(updatedBy, routeInDb.UpdatedBy);

            // SignalR verification removed since service no longer uses SignalR
        }

        [Fact]
        public async Task DeleteRouteAsync_ShouldDeleteFromDatabaseAndNotifyHub()
        {
            // Arrange
            var routeEntityToDelete = SeedRouteDirectly("RouteToDelete");

            // Act
            var deleteResult = await _routeConfigService.DeleteRouteAsync(routeEntityToDelete.Id);

            // Assert
            Assert.True(deleteResult);
            var routeInDb = await DbContext.RouteConfigs.FindAsync(routeEntityToDelete.Id);
            Assert.Null(routeInDb);

            // SignalR verification removed since service no longer uses SignalR
        }

        [Fact]
        public async Task GetRouteByIdAsync_ShouldReturnCorrectRoute()
        {
            // Arrange
            var routeEntity = SeedRouteDirectly("RouteToGet");

            // Act
            var result = await _routeConfigService.GetRouteByIdAsync(routeEntity.Id);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(routeEntity.Id, result.Id);
            Assert.Equal(routeEntity.Name, result.Name);
        }

        [Fact]
        public async Task GetAllRoutesAsync_ShouldReturnAllRoutes()
        {
            // Arrange
            SeedRouteDirectly("Route1");
            SeedRouteDirectly("Route2");

            // Act
            var results = await _routeConfigService.GetAllRoutesAsync();

            // Assert
            Assert.NotNull(results);
            Assert.Equal(2, results.Count());
        }

        [Fact]
        public async Task ToggleRouteStatusAsync_ShouldUpdateStatusAndNotifyHub()
        {
            // Arrange
            var routeEntity = SeedRouteDirectly("RouteToToggle", isActive: true);
            Assert.True(routeEntity.IsActive);

            // Act: Deactivate
            var deactivateResult = await _routeConfigService.ToggleRouteStatusAsync(routeEntity.Id, false);

            // Assert: Deactivate
            Assert.True(deactivateResult);
            var routeInDb = await DbContext.RouteConfigs.FindAsync(routeEntity.Id);
            Assert.NotNull(routeInDb);
            Assert.False(routeInDb.IsActive);

            // SignalR verification removed since service no longer uses SignalR


            // Act: Activate
            var activateResult = await _routeConfigService.ToggleRouteStatusAsync(routeEntity.Id, true);

            // Assert: Activate
            Assert.True(activateResult);
            routeInDb = await DbContext.RouteConfigs.FindAsync(routeEntity.Id); // Re-fetch
            Assert.NotNull(routeInDb);
            Assert.True(routeInDb.IsActive);

            // SignalR verification removed since service no longer uses SignalR
        }
    }
}
