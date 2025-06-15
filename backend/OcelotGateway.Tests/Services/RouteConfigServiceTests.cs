using AutoMapper;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Moq;
using OcelotGateway.Application.DTOs;
using OcelotGateway.Application.Interfaces;
using OcelotGateway.Application.Mappings; // Assuming MappingProfile is here
using OcelotGateway.Application.Services;
using OcelotGateway.Domain.Entities;
using OcelotGateway.Domain.ValueObjects;
using OcelotGateway.Infrastructure.Repositories;
using OcelotGateway.Tests.Common;
using OcelotGateway.WebApi.Hubs; // For ConfigurationHub
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
        private readonly Mock<IHubContext<ConfigurationHub>> _mockHubContext;
        private readonly Mock<IHubClients> _mockHubClients;
        private readonly Mock<IClientProxy> _mockClientProxy;
        // IMapper is not injected into RouteConfigService, it uses a static MapToDto method.

        public RouteConfigServiceTests()
        {
            // 1. Setup Mocks for SignalR
            _mockClientProxy = new Mock<IClientProxy>();
            _mockHubClients = new Mock<IHubClients>();
            _mockHubClients.Setup(clients => clients.All).Returns(_mockClientProxy.Object);
            _mockHubContext = new Mock<IHubContext<ConfigurationHub>>();
            _mockHubContext.Setup(hub => hub.Clients).Returns(_mockHubClients.Object);

            // 2. Setup AutoMapper - Not strictly needed for service instantiation if not injected,
            // but good for verifying DTO mapping if the service returns DTOs created by AutoMapper.
            // RouteConfigService uses a static MapToDto, so an IMapper instance isn't passed to its constructor.
            // var mapperConfig = new MapperConfiguration(cfg =>
            // {
            //     cfg.AddProfile<MappingProfile>();
            // });
            // _mapper = mapperConfig.CreateMapper();


            // 3. Setup Repository
            var routeRepository = new RouteConfigRepository(DbContext); // DbContext comes from DatabaseTestBase

            // 4. Instantiate the Service
            // Based on previous steps, RouteConfigService constructor takes (IRouteConfigRepository, IHubContext<ConfigurationHub>)
            _routeConfigService = new RouteConfigService(routeRepository, _mockHubContext.Object);
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
        public async Task CreateRouteAsync_ShouldSaveToDatabaseAndNotifyHub()
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

            _mockClientProxy.Verify(
                c => c.SendCoreAsync(
                    "ConfigurationChanged",
                    It.Is<object[]>(o => o != null && o.Length == 1 &&
                                         o[0].GetType().GetProperty("Type").GetValue(o[0]).ToString() == "RouteCreated" &&
                                         o[0].GetType().GetProperty("RouteName").GetValue(o[0]).ToString() == dto.Name &&
                                         o[0].GetType().GetProperty("Environment").GetValue(o[0]).ToString() == dto.Environment),
                    default(CancellationToken)),
                Times.Once);
        }

        [Fact]
        public async Task UpdateRouteAsync_ShouldUpdateDatabaseAndNotifyHub()
        {
            // Arrange
            var initialEntity = SeedRouteDirectly("InitialRouteForUpdate");
            _mockClientProxy.Invocations.Clear();

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

            _mockClientProxy.Verify(
                c => c.SendCoreAsync(
                    "ConfigurationChanged",
                     It.Is<object[]>(o => o != null && o.Length == 1 &&
                                          o[0].GetType().GetProperty("Type").GetValue(o[0]).ToString() == "RouteUpdated" &&
                                          o[0].GetType().GetProperty("RouteName").GetValue(o[0]).ToString() == updateDto.Name &&
                                          o[0].GetType().GetProperty("Environment").GetValue(o[0]).ToString() == initialEntity.Environment),
                    default(CancellationToken)),
                Times.Once);
        }

        [Fact]
        public async Task DeleteRouteAsync_ShouldDeleteFromDatabaseAndNotifyHub()
        {
            // Arrange
            var routeEntityToDelete = SeedRouteDirectly("RouteToDelete");
            _mockClientProxy.Invocations.Clear();

            // Act
            var deleteResult = await _routeConfigService.DeleteRouteAsync(routeEntityToDelete.Id);

            // Assert
            Assert.True(deleteResult);
            var routeInDb = await DbContext.RouteConfigs.FindAsync(routeEntityToDelete.Id);
            Assert.Null(routeInDb);

            _mockClientProxy.Verify(
                c => c.SendCoreAsync(
                    "ConfigurationChanged",
                     It.Is<object[]>(o => o != null && o.Length == 1 &&
                                          o[0].GetType().GetProperty("Type").GetValue(o[0]).ToString() == "RouteDeleted" &&
                                          o[0].GetType().GetProperty("RouteName").GetValue(o[0]).ToString() == routeEntityToDelete.Name &&
                                          o[0].GetType().GetProperty("Environment").GetValue(o[0]).ToString() == routeEntityToDelete.Environment),
                    default(CancellationToken)),
                Times.Once);
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
            _mockClientProxy.Invocations.Clear();

            // Act: Deactivate
            var deactivateResult = await _routeConfigService.ToggleRouteStatusAsync(routeEntity.Id, false);

            // Assert: Deactivate
            Assert.True(deactivateResult);
            var routeInDb = await DbContext.RouteConfigs.FindAsync(routeEntity.Id);
            Assert.NotNull(routeInDb);
            Assert.False(routeInDb.IsActive);

            _mockClientProxy.Verify(
                c => c.SendCoreAsync(
                    "ConfigurationChanged",
                     It.Is<object[]>(o => o != null && o.Length == 1 &&
                                          o[0].GetType().GetProperty("Type").GetValue(o[0]).ToString() == "RouteStatusChanged" &&
                                          o[0].GetType().GetProperty("RouteName").GetValue(o[0]).ToString() == routeEntity.Name &&
                                          (bool)o[0].GetType().GetProperty("IsActive").GetValue(o[0]) == false),
                    default(CancellationToken)),
                Times.Once);

            _mockClientProxy.Invocations.Clear();

            // Act: Activate
            var activateResult = await _routeConfigService.ToggleRouteStatusAsync(routeEntity.Id, true);

            // Assert: Activate
            Assert.True(activateResult);
            routeInDb = await DbContext.RouteConfigs.FindAsync(routeEntity.Id); // Re-fetch
            Assert.NotNull(routeInDb);
            Assert.True(routeInDb.IsActive);

            _mockClientProxy.Verify(
                c => c.SendCoreAsync(
                    "ConfigurationChanged",
                     It.Is<object[]>(o => o != null && o.Length == 1 &&
                                          o[0].GetType().GetProperty("Type").GetValue(o[0]).ToString() == "RouteStatusChanged" &&
                                          o[0].GetType().GetProperty("RouteName").GetValue(o[0]).ToString() == routeEntity.Name &&
                                          (bool)o[0].GetType().GetProperty("IsActive").GetValue(o[0]) == true),
                    default(CancellationToken)),
                Times.Once);
        }
    }
}
