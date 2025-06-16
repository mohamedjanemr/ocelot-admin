using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Ocelot.Configuration.File;
using Ocelot.Configuration.Repository;
using Ocelot.Responses;
using OcelotGateway.Domain.Entities;
using OcelotGateway.Domain.Interfaces;
using OcelotGateway.Domain.ValueObjects;
using OcelotGateway.Gateway.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OcelotGateway.Gateway.Providers
{
    /// <summary>
    /// Custom Ocelot configuration provider that loads configuration from the database
    /// </summary>
    public class DatabaseConfigurationProvider : IFileConfigurationRepository
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly string _environment;
        private readonly ILogger<DatabaseConfigurationProvider> _logger;
        private readonly ConfigurationCacheService _cacheService;

        public DatabaseConfigurationProvider(
            IServiceProvider serviceProvider, 
            ConfigurationCacheService cacheService,
            ILogger<DatabaseConfigurationProvider> logger,
            string environment = "Development")
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _environment = environment;
        }

        public async Task<Response<FileConfiguration>> Get()
        {
            try
            {
                _logger.LogInformation("Loading Ocelot configuration for environment: {Environment}", _environment);
                
                var fileConfig = await _cacheService.GetConfigurationAsync(_environment);

                _logger.LogInformation("Configuration loaded successfully for environment: {Environment} with {RouteCount} routes", 
                    _environment, fileConfig.Routes.Count);

                return new OkResponse<FileConfiguration>(fileConfig);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading configuration for environment: {Environment}", _environment);
                return new ErrorResponse<FileConfiguration>(new List<Ocelot.Errors.Error>());
            }
        }

        public async Task<Response> Set(FileConfiguration fileConfiguration)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var configRepository = scope.ServiceProvider.GetRequiredService<IConfigurationVersionRepository>();

                // Create a new configuration version
                var version = $"{DateTime.UtcNow:yyyy.MM.dd.HH.mm.ss}";
                var configVersion = new ConfigurationVersion(
                    version,
                    $"Configuration updated at {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}",
                    _environment,
                    "System"
                );

                // Convert Ocelot routes to domain model
                foreach (var route in fileConfiguration.Routes)
                {
                    var upstreamHttpMethod = route.UpstreamHttpMethod?.FirstOrDefault() ?? "GET";
                    
                    var routeConfig = new RouteConfig(
                        route.Key ?? $"Route-{Guid.NewGuid()}",
                        route.DownstreamPathTemplate,
                        route.UpstreamPathTemplate,
                        upstreamHttpMethod,
                        route.DownstreamScheme,
                        route.DownstreamHostAndPorts.Select(h => new HostAndPort(h.Host, h.Port)).ToList(),
                        _environment,
                        "System"
                    );

                    configVersion.AddRouteConfiguration(routeConfig);
                }

                // Save the configuration
                await configRepository.AddAsync(configVersion);

                // Activate the configuration
                configVersion.Publish("System");
                await configRepository.UpdateAsync(configVersion);

                // Deactivate previous active configurations
                var activeConfigs = await configRepository.GetByEnvironmentAsync(_environment);
                foreach (var config in activeConfigs.Where(c => c.IsActive && c.Id != configVersion.Id))
                {
                    config.Unpublish();
                    await configRepository.UpdateAsync(config);
                }

                return new OkResponse();
            }
            catch (Exception)
            {
                return new ErrorResponse(new List<Ocelot.Errors.Error>());
            }
        }
    }
} 