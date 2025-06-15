using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Configuration; // Added for IConfiguration
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Ocelot.Cache;
using Ocelot.Configuration.Repository;
using OcelotGateway.Domain.Interfaces; // For IRouteConfigRepository
using System;
using System.Linq; // Added for Select in Error logging
using System.Threading;
using System.Threading.Tasks;

namespace OcelotGateway.Gateway.Services
{
    public class SignalRService : IHostedService, IDisposable
    {
        private readonly ILogger<SignalRService> _logger;
        private readonly IOcelotCache<Ocelot.Configuration.File.FileConfiguration> _fileConfigurationCache;
        private readonly IFileConfigurationRepository _fileConfigurationRepository;
        private readonly IServiceProvider _serviceProvider;
        private readonly IConfiguration _configuration; // Added
        private HubConnection _hubConnection;
        private readonly string _hubUrl; // To store the resolved Hub URL

        public SignalRService(
            ILogger<SignalRService> logger,
            IOcelotCache<Ocelot.Configuration.File.FileConfiguration> fileConfigurationCache,
            IFileConfigurationRepository fileConfigurationRepository,
            IServiceProvider serviceProvider,
            IConfiguration configuration) // Added IConfiguration
        {
            _logger = logger;
            _fileConfigurationCache = fileConfigurationCache;
            _fileConfigurationRepository = fileConfigurationRepository;
            _serviceProvider = serviceProvider;
            _configuration = configuration; // Stored

            // Read Hub URL from configuration
            _hubUrl = _configuration["SignalR:HubUrl"];
            if (string.IsNullOrEmpty(_hubUrl))
            {
                _logger.LogWarning("SignalR:HubUrl not configured. Using default: http://localhost:5000/configurationhub");
                _hubUrl = "http://localhost:5000/configurationhub"; // Fallback to a default
            }

            _logger.LogInformation("SignalRService will connect to Hub URL: {HubUrl}", _hubUrl);

            _hubConnection = new HubConnectionBuilder()
                .WithUrl(_hubUrl) // Use the configured or default URL
                .WithAutomaticReconnect()
                .Build();

            RegisterHandlers();
        }

        private void RegisterHandlers()
        {
            _hubConnection.On<object>("ConfigurationChanged", (payload) =>
            {
                _logger.LogInformation("SignalR: Received 'ConfigurationChanged' notification. Payload: {Payload}", payload?.ToString());
                InvalidateCacheAndReloadConfiguration();
            });
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _hubConnection.Closed += async (error) =>
            {
                _logger.LogError(error, "SignalR connection closed to {HubUrl}. Attempting to reconnect...", _hubUrl);
                await Task.Delay(new Random().Next(0, 5) * 1000, cancellationToken); // Random delay before reconnect
                await ConnectWithRetryAsync(cancellationToken);
            };

            _logger.LogInformation("Attempting to connect to SignalR hub at {HubUrl}", _hubUrl);
            await ConnectWithRetryAsync(cancellationToken);
        }

        private async Task ConnectWithRetryAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    if (_hubConnection.State == HubConnectionState.Disconnected)
                    {
                        await _hubConnection.StartAsync(cancellationToken);
                        _logger.LogInformation("Successfully connected to SignalR hub at {HubUrl}", _hubUrl);
                        return; // Exit retry loop on success
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to connect to SignalR hub at {HubUrl}. Retrying in 5 seconds...", _hubUrl);
                }

                if (cancellationToken.IsCancellationRequested) break;
                await Task.Delay(5000, cancellationToken);
            }
        }

        private async void InvalidateCacheAndReloadConfiguration()
        {
            _logger.LogInformation("SignalR: Invalidating Ocelot cache and reloading configuration due to notification.");

            _fileConfigurationCache.Clear();
            _logger.LogInformation("Ocelot cache cleared. Ocelot should reload configuration on next request or poll.");

            try
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    var internalConfigRepo = scope.ServiceProvider.GetService<IInternalConfigurationRepository>();
                    var ocelotFileConfig = await _fileConfigurationRepository.Get();

                    if (ocelotFileConfig?.Data != null && internalConfigRepo != null)
                    {
                        var setResult = internalConfigRepo.Set(ocelotFileConfig.Data);
                        if (setResult.IsError)
                        {
                            _logger.LogError("SignalR: Error setting Ocelot internal configuration after cache clear: {Errors}", string.Join(", ", setResult.Errors.Select(e => e.Message)));
                        }
                        else
                        {
                            _logger.LogInformation("SignalR: Successfully updated Ocelot internal configuration via IInternalConfigurationRepository after cache clear.");
                        }
                    }
                    else
                    {
                        _logger.LogWarning("SignalR: Could not retrieve FileConfiguration from DB or IInternalConfigurationRepository not found, during proactive reload.");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SignalR: Exception during proactive Ocelot internal configuration update.");
            }
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            if (_hubConnection != null)
            {
                _logger.LogInformation("SignalRService stopping. Disconnecting from {HubUrl}", _hubUrl);
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
