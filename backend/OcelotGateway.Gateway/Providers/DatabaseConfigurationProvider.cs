using Microsoft.Extensions.DependencyInjection;
using Ocelot.Configuration.File;
using Ocelot.Configuration.Repository;
using Ocelot.Responses;
using OcelotGateway.Domain.Entities;
using OcelotGateway.Domain.Interfaces;
using OcelotGateway.Domain.ValueObjects;
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

        public DatabaseConfigurationProvider(IServiceProvider serviceProvider, string environment = "Development")
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _environment = environment;
        }

        public async Task<Response<FileConfiguration>> Get()
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var configRepository = scope.ServiceProvider.GetRequiredService<IConfigurationVersionRepository>();

                // Get active configuration for the current environment
                var activeConfig = await configRepository.GetActiveConfigurationAsync(_environment);
                
                if (activeConfig == null)
                {
                    return new ErrorResponse<FileConfiguration>(new List<Ocelot.Errors.Error>());
                }

                // Convert domain model to Ocelot configuration
                var fileConfig = new FileConfiguration
                {
                    GlobalConfiguration = new FileGlobalConfiguration
                    {
                        BaseUrl = null, // This would be set from a configuration setting
                        RequestIdKey = "OcRequestId",
                        DownstreamScheme = "http",
                        HttpHandlerOptions = new FileHttpHandlerOptions
                        {
                            AllowAutoRedirect = false,
                            UseCookieContainer = false,
                            UseTracing = false
                        }
                    },
                    Routes = new List<FileRoute>()
                };

                // Add routes to the configuration
                foreach (var route in activeConfig.RouteConfigurations.Where(r => r.IsActive))
                {
                    var fileRoute = new FileRoute
                    {
                        DownstreamPathTemplate = route.DownstreamPathTemplate,
                        UpstreamPathTemplate = route.UpstreamPathTemplate,
                        DownstreamScheme = route.DownstreamScheme,
                        Key = route.Name,
                        DownstreamHostAndPorts = new List<FileHostAndPort>()
                    };

                    // Add downstream hosts and ports
                    foreach (var hostAndPort in route.DownstreamHostAndPorts)
                    {
                        fileRoute.DownstreamHostAndPorts.Add(new FileHostAndPort
                        {
                            Host = hostAndPort.Host,
                            Port = hostAndPort.Port
                        });
                    }

                    // Add HTTP methods
                    if (string.IsNullOrEmpty(route.UpstreamHttpMethod))
                    {
                        if (route.UpstreamHttpMethods != null && route.UpstreamHttpMethods.Any())
                        {
                            fileRoute.UpstreamHttpMethod = new List<string>();
                            foreach (var method in route.UpstreamHttpMethods)
                            {
                                fileRoute.UpstreamHttpMethod.Add(method);
                            }
                        }
                        else
                        {
                            fileRoute.UpstreamHttpMethod = new List<string> { "GET" };
                        }
                    }
                    else
                    {
                        fileRoute.UpstreamHttpMethod = new List<string> { route.UpstreamHttpMethod };
                    }

                    fileConfig.Routes.Add(fileRoute);
                }

                return new OkResponse<FileConfiguration>(fileConfig);
            }
            catch (Exception)
            {
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
                    $"Configuration updated at {DateTime.UtcNow}",
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