using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite; // For SqliteConnection
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OcelotGateway.Application.DTOs; // For CreateRouteConfigDto, UpdateRouteConfigDto, RouteConfigDto
using OcelotGateway.Infrastructure.Data;
using OcelotGateway.Tests.Factories;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json; // For PostAsJsonAsync, ReadFromJsonAsync
using System.Threading.Tasks;
using Xunit;

namespace OcelotGateway.Tests.Integration
{
    public class RealtimeConfigurationUpdateTests : IAsyncLifetime // Implements IAsyncLifetime for shared resources
    {
        private SqliteConnection? _sharedConnection;

        private CustomWebApplicationFactory<OcelotGateway.WebApi.Program>? _webApiFactory;
        private GatewayWebApplicationFactory? _gatewayFactory;

        private HttpClient? _webApiClient;
        private HttpClient? _gatewayClient;

        // Test constants
        private const string TestEnvironment = "Development"; // Should match environment used in DTOs
        private const int InitialSignalRConnectionDelayMs = 3000; // Time for initial SignalR connection
        private const int PropagationDelayMs = 5000; // Time for changes to propagate and gateway to reload


        public async Task InitializeAsync()
        {
            _sharedConnection = new SqliteConnection("DataSource=:memory:;Cache=Shared");
            await _sharedConnection.OpenAsync(); // Keep connection open

            // Ensure schema is created once for the shared connection
            var dbContextOptionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlite(_sharedConnection);
            using (var context = new ApplicationDbContext(dbContextOptionsBuilder.Options))
            {
                await context.Database.EnsureCreatedAsync();
            }

            _webApiFactory = new CustomWebApplicationFactory<OcelotGateway.WebApi.Program>(_sharedConnection);

            var tempWebApiClient = _webApiFactory.CreateClient(); // Create client to start the server and get BaseAddress
            var webApiBaseAddress = tempWebApiClient.BaseAddress?.ToString() ?? "http://localhost:5000"; // Fallback

            _gatewayFactory = new GatewayWebApplicationFactory(_sharedConnection, webApiBaseAddress.TrimEnd('/') + "/configurationhub");

            _webApiClient = _webApiFactory.CreateClient(); // Re-create or use the temp one if preferred.
            _gatewayClient = _gatewayFactory.CreateClient();

            await Task.Delay(InitialSignalRConnectionDelayMs);
        }

        public async Task DisposeAsync()
        {
            _gatewayClient?.Dispose();
            _webApiClient?.Dispose();
            _gatewayFactory?.Dispose();
            _webApiFactory?.Dispose();
            if (_sharedConnection != null)
            {
                await _sharedConnection.CloseAsync();
                await _sharedConnection.DisposeAsync();
            }
        }

        [Fact]
        public async Task RouteCreation_EndToEnd_UpdatesGatewayViaSignalR()
        {
            Assert.NotNull(_webApiClient);
            Assert.NotNull(_gatewayClient);

            var createRouteDto = new CreateRouteConfigDto
            {
                Name = "E2ECreateTestRoute",
                UpstreamPathTemplate = "/e2e-create-test",
                DownstreamPathTemplate = "/todos/1",
                DownstreamScheme = "https",
                DownstreamHostAndPorts = new List<HostAndPortDto> { new HostAndPortDto { Host = "jsonplaceholder.typicode.com", Port = 443 } },
                UpstreamHttpMethod = "GET",
                Environment = TestEnvironment
            };

            var apiResponse = await _webApiClient.PostAsJsonAsync("/api/RouteConfig", createRouteDto);
            apiResponse.EnsureSuccessStatusCode();

            await Task.Delay(PropagationDelayMs);

            HttpResponseMessage gatewayResponse = new HttpResponseMessage();
            try
            {
                gatewayResponse = await _gatewayClient.GetAsync(createRouteDto.UpstreamPathTemplate);
                gatewayResponse.EnsureSuccessStatusCode();
                var content = await gatewayResponse.Content.ReadAsStringAsync();
                Assert.Contains("delectus aut autem", content, StringComparison.OrdinalIgnoreCase);
            }
            catch (HttpRequestException ex)
            {
                Assert.True(false, $"Gateway request failed. Status: {gatewayResponse.StatusCode}. Exception: {ex.Message}.");
            }
        }

        [Fact]
        public async Task RouteUpdate_EndToEnd_UpdatesGatewayBehavior()
        {
            Assert.NotNull(_webApiClient);
            Assert.NotNull(_gatewayClient);

            var initialUpstreamPath = "/e2e-update-test";
            var initialRouteDto = new CreateRouteConfigDto
            {
                Name = "E2EUpdateTestRouteInitial",
                UpstreamPathTemplate = initialUpstreamPath,
                DownstreamPathTemplate = "/todos/1",
                DownstreamScheme = "https",
                DownstreamHostAndPorts = new List<HostAndPortDto> { new HostAndPortDto { Host = "jsonplaceholder.typicode.com", Port = 443 } },
                UpstreamHttpMethod = "GET",
                Environment = TestEnvironment
            };

            var createResponse = await _webApiClient.PostAsJsonAsync("/api/RouteConfig", initialRouteDto);
            createResponse.EnsureSuccessStatusCode();
            var createdRoute = await createResponse.Content.ReadFromJsonAsync<RouteConfigDto>();
            Assert.NotNull(createdRoute);

            await Task.Delay(PropagationDelayMs);

            var initialGatewayResponse = await _gatewayClient.GetAsync(initialUpstreamPath);
            initialGatewayResponse.EnsureSuccessStatusCode();
            var initialContent = await initialGatewayResponse.Content.ReadAsStringAsync();
            Assert.Contains("\"id\": 1", initialContent);

            var updateDto = new UpdateRouteConfigDto
            {
                Name = "E2EUpdateTestRouteUpdated",
                DownstreamPathTemplate = "/todos/2",
                UpstreamPathTemplate = initialRouteDto.UpstreamPathTemplate,
                UpstreamHttpMethod = initialRouteDto.UpstreamHttpMethod,
                DownstreamScheme = initialRouteDto.DownstreamScheme,
                DownstreamHostAndPorts = initialRouteDto.DownstreamHostAndPorts,
                ServiceName = initialRouteDto.ServiceName
            };

            var updateResponse = await _webApiClient.PutAsJsonAsync($"/api/RouteConfig/{createdRoute.Id}", updateDto);
            updateResponse.EnsureSuccessStatusCode();

            await Task.Delay(PropagationDelayMs);

            var updatedGatewayResponse = await _gatewayClient.GetAsync(initialUpstreamPath);
            updatedGatewayResponse.EnsureSuccessStatusCode();
            var updatedContent = await updatedGatewayResponse.Content.ReadAsStringAsync();
            Assert.Contains("\"id\": 2", updatedContent);
        }

        [Fact]
        public async Task RouteDeletion_EndToEnd_MakesRouteUnavailableOnGateway()
        {
            Assert.NotNull(_webApiClient);
            Assert.NotNull(_gatewayClient);

            var upstreamPathToDelete = "/e2e-delete-test";
            var routeToDeleteDto = new CreateRouteConfigDto
            {
                Name = "E2EDeleteTestRoute",
                UpstreamPathTemplate = upstreamPathToDelete,
                DownstreamPathTemplate = "/posts/1",
                DownstreamScheme = "https",
                DownstreamHostAndPorts = new List<HostAndPortDto> { new HostAndPortDto { Host = "jsonplaceholder.typicode.com", Port = 443 } },
                UpstreamHttpMethod = "GET",
                Environment = TestEnvironment
            };

            var createResponse = await _webApiClient.PostAsJsonAsync("/api/RouteConfig", routeToDeleteDto);
            createResponse.EnsureSuccessStatusCode();
            var createdRoute = await createResponse.Content.ReadFromJsonAsync<RouteConfigDto>();
            Assert.NotNull(createdRoute);

            await Task.Delay(PropagationDelayMs);

            var initialGatewayResponse = await _gatewayClient.GetAsync(upstreamPathToDelete);
            initialGatewayResponse.EnsureSuccessStatusCode();

            var deleteResponse = await _webApiClient.DeleteAsync($"/api/RouteConfig/{createdRoute.Id}");
            deleteResponse.EnsureSuccessStatusCode();

            await Task.Delay(PropagationDelayMs);

            var finalGatewayResponse = await _gatewayClient.GetAsync(upstreamPathToDelete);
            Assert.Equal(System.Net.HttpStatusCode.NotFound, finalGatewayResponse.StatusCode);
        }
    }
}
