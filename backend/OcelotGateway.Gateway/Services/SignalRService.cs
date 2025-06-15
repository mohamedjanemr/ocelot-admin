using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Ocelot.Cache;
using Ocelot.Configuration.Repository;
using OcelotGateway.Domain.Interfaces; // For IRouteConfigRepository
using System;
using System.Threading;
using System.Threading.Tasks;

namespace OcelotGateway.Gateway.Services
{
    public class SignalRService : IHostedService, IDisposable
    {
        private readonly ILogger<SignalRService> _logger;
        private readonly IOcelotCache<Ocelot.Configuration.File.FileConfiguration> _fileConfigurationCache;
        private readonly IFileConfigurationRepository _fileConfigurationRepository; // This is our DatabaseConfigurationProvider
        private readonly IServiceProvider _serviceProvider;
        private HubConnection _hubConnection;
        private const string ConfigurationHubUrl = "http://localhost:5000/configurationhub"; // As determined from WebApi launchSettings

        public SignalRService(
            ILogger<SignalRService> logger,
            IOcelotCache<Ocelot.Configuration.File.FileConfiguration> fileConfigurationCache,
            IFileConfigurationRepository fileConfigurationRepository,
            IServiceProvider serviceProvider)
        {
            _logger = logger;
            _fileConfigurationCache = fileConfigurationCache;
            _fileConfigurationRepository = fileConfigurationRepository;
            _serviceProvider = serviceProvider;

            _hubConnection = new HubConnectionBuilder()
                .WithUrl(ConfigurationHubUrl)
                .WithAutomaticReconnect()
                .Build();

            RegisterHandlers();
        }

        private void RegisterHandlers()
        {
            // Standardized handler for all configuration changes
            _hubConnection.On<object>("ConfigurationChanged", (payload) => // Changed from specific messages to a general one
            {
                // Log the received payload. For complex objects, consider serializing to JSON.
                _logger.LogInformation("SignalR: Received 'ConfigurationChanged' notification. Payload: {Payload}", payload?.ToString());

                // Invalidate cache and trigger reload
                InvalidateCacheAndReloadConfiguration();
            });

            // Commented out or removed older, more specific handlers:
            // _hubConnection.On<string>("ReceiveConfigurationUpdate", (message) =>
            // {
            //     _logger.LogInformation("SignalR: Received configuration update notification: {Message}", message);
            //     InvalidateCacheAndReloadConfiguration();
            // });
            // _hubConnection.On<string>("ReceiveRouteConfigCreated", (routeName) =>
            // {
            //     _logger.LogInformation("SignalR: RouteConfig created: {RouteName}", routeName);
            //     InvalidateCacheAndReloadConfiguration();
            // });
            // _hubConnection.On<string>("ReceiveRouteConfigUpdated", (routeName) =>
            // {
            //     _logger.LogInformation("SignalR: RouteConfig updated: {RouteName}", routeName);
            //     InvalidateCacheAndReloadConfiguration();
            // });
            // _hubConnection.On<string>("ReceiveRouteConfigDeleted", (routeName) =>
            // {
            //     _logger.LogInformation("SignalR: RouteConfig deleted: {RouteName}", routeName);
            //     InvalidateCacheAndReloadConfiguration();
            // });
            // _hubConnection.On<string>("ReceiveGlobalConfigUpdated", (configId) =>
            // {
            //     _logger.LogInformation("SignalR: GlobalConfig updated: {ConfigId}", configId);
            //     InvalidateCacheAndReloadConfiguration();
            // });
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _hubConnection.Closed += async (error) =>
            {
                _logger.LogError(error, "SignalR connection closed. Attempting to reconnect...");
                await Task.Delay(new Random().Next(0, 5) * 1000, cancellationToken);
                await ConnectWithRetryAsync(cancellationToken);
            };

            _logger.LogInformation("Attempting to connect to SignalR hub at {HubUrl}", ConfigurationHubUrl);
            await ConnectWithRetryAsync(cancellationToken);
        }

        private async Task ConnectWithRetryAsync(CancellationToken cancellationToken)
        {
            while (true)
            {
                try
                {
                    if (_hubConnection.State == HubConnectionState.Disconnected)
                    {
                        await _hubConnection.StartAsync(cancellationToken);
                        _logger.LogInformation("Successfully connected to SignalR hub.");
                        break;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to connect to SignalR hub. Retrying in 5 seconds...");
                }
                await Task.Delay(5000, cancellationToken);
            }
        }

        private async void InvalidateCacheAndReloadConfiguration()
        {
            _logger.LogInformation("SignalR: Invalidating Ocelot cache and reloading configuration.");

            // Invalidate the Ocelot internal cache for FileConfiguration
            _fileConfigurationCache.Clear(); // This clears the cached version of the overall file configuration

            // Ocelot's DatabaseConfigurationProvider (our IFileConfigurationRepository)
            // needs to provide the updated configuration when Ocelot requests it.
            // The cache clear above should trigger Ocelot to re-fetch.
            // We might need to explicitly tell Ocelot to reload its internal config if just clearing cache isn't enough.
            // For now, relying on Ocelot's polling or cache miss to re-fetch from IFileConfigurationRepository.

            // To ensure the IFileConfigurationRepository itself has the latest from DB,
            // it should ideally fetch fresh data in its Get() method.
            // If IFileConfigurationRepository caches, that cache would also need invalidation,
            // but its current implementation in the repo fetches directly from DB via RouteConfigRepository.

            // As a more direct approach if Ocelot doesn't auto-reload:
            // We might need to resolve Ocelot's IInternalConfigurationRepository and call Set.
            // However, direct manipulation of Ocelot's internal state can be risky.
            // The most straightforward way provided by Ocelot is often through its /administration API
            // to trigger a re-configuration, but that's an HTTP call.

            // For now, we assume clearing _fileConfigurationCache is the main step.
            // Further investigation might be needed if Ocelot doesn't pick up changes automatically after this.
            _logger.LogInformation("Ocelot cache cleared. Ocelot should reload configuration on next request or poll.");

            // Additionally, let's try to resolve the IInternalConfigurationRepository
            // and set it with the latest configuration from our repository.
            // This is a more proactive way to force Ocelot to update.
            try
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    var internalConfigRepo = scope.ServiceProvider.GetService<Ocelot.Configuration.Repository.IInternalConfigurationRepository>();
                    var ocelotConfig = await _fileConfigurationRepository.Get(); // Get latest from DB

                    if (ocelotConfig?.Data != null && internalConfigRepo != null)
                    {
                        var setResult = internalConfigRepo.Set(ocelotConfig.Data);
                        if (setResult.IsError)
                        {
                            _logger.LogError("SignalR: Error setting Ocelot internal configuration: {Errors}", string.Join(", ", setResult.Errors.Select(e => e.Message)));
                        }
                        else
                        {
                            _logger.LogInformation("SignalR: Successfully updated Ocelot internal configuration via IInternalConfigurationRepository.");
                        }
                    }
                    else
                    {
                        _logger.LogWarning("SignalR: Could not retrieve configuration from DB or IInternalConfigurationRepository not found.");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SignalR: Exception while trying to update Ocelot internal configuration.");
            }
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            if (_hubConnection != null)
            {
                await _hubConnection.StopAsync(cancellationToken);
                await _hubConnection.DisposeAsync();
            }
        }

        public void Dispose()
        {
            _hubConnection?.DisposeAsync().AsTask().Wait();
        }
    }
}
