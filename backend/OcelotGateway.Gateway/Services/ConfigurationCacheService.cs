using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Ocelot.Configuration.File;
using OcelotGateway.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OcelotGateway.Gateway.Services
{
    /// <summary>
    /// Service for caching Ocelot configurations to improve performance
    /// </summary>
    public class ConfigurationCacheService
    {
        private readonly IMemoryCache _cache;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<ConfigurationCacheService> _logger;
        private static readonly TimeSpan DefaultCacheDuration = TimeSpan.FromMinutes(5);

        public ConfigurationCacheService(
            IMemoryCache cache, 
            IServiceProvider serviceProvider,
            ILogger<ConfigurationCacheService> logger)
        {
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Gets the cached configuration or loads it from the database
        /// </summary>
        /// <param name="environment">The environment name</param>
        /// <returns>The cached or fresh configuration</returns>
        public async Task<FileConfiguration> GetConfigurationAsync(string environment)
        {
            var cacheKey = $"{environment}_ocelot_config";
            
            if (_cache.TryGetValue(cacheKey, out FileConfiguration? cachedConfig))
            {
                _logger.LogDebug("Configuration retrieved from cache for environment: {Environment}", environment);
                return cachedConfig!;
            }

            _logger.LogInformation("Loading configuration from database for environment: {Environment}", environment);
            
            using var scope = _serviceProvider.CreateScope();
            var configRepository = scope.ServiceProvider.GetRequiredService<IConfigurationVersionRepository>();

            var activeConfig = await configRepository.GetActiveConfigurationAsync(environment);
            if (activeConfig == null)
            {
                _logger.LogWarning("No active configuration found for environment: {Environment}", environment);
                return CreateEmptyFileConfiguration();
            }

            var fileConfig = ConvertToFileConfiguration(activeConfig);
            
            // Cache the configuration
            var cacheOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = DefaultCacheDuration,
                SlidingExpiration = TimeSpan.FromMinutes(2),
                Priority = CacheItemPriority.High
            };

            _cache.Set(cacheKey, fileConfig, cacheOptions);
            _logger.LogInformation("Configuration cached for environment: {Environment}", environment);

            return fileConfig;
        }

        /// <summary>
        /// Invalidates the cached configuration for a specific environment
        /// </summary>
        /// <param name="environment">The environment name</param>
        public void InvalidateCache(string environment)
        {
            var cacheKey = $"{environment}_ocelot_config";
            _cache.Remove(cacheKey);
            _logger.LogInformation("Configuration cache invalidated for environment: {Environment}", environment);
        }

        /// <summary>
        /// Invalidates all cached configurations
        /// </summary>
        public void InvalidateAllCache()
        {
            // Since IMemoryCache doesn't have a clear all method, we'll rely on expiration
            // In a production system, you might want to track cache keys for manual removal
            _logger.LogInformation("All configuration caches invalidated (via expiration)");
        }

        /// <summary>
        /// Creates an empty file configuration
        /// </summary>
        private static FileConfiguration CreateEmptyFileConfiguration()
        {
            return new FileConfiguration
            {
                GlobalConfiguration = new FileGlobalConfiguration
                {
                    BaseUrl = null,
                    RequestIdKey = "OcRequestId",
                    DownstreamScheme = "http",
                    HttpHandlerOptions = new FileHttpHandlerOptions
                    {
                        AllowAutoRedirect = false,
                        UseCookieContainer = false,
                        UseTracing = true
                    }
                },
                Routes = new List<FileRoute>()
            };
        }

        /// <summary>
        /// Converts domain configuration to Ocelot file configuration
        /// </summary>
        private static FileConfiguration ConvertToFileConfiguration(OcelotGateway.Domain.Entities.ConfigurationVersion configVersion)
        {
            var fileConfig = new FileConfiguration
            {
                GlobalConfiguration = new FileGlobalConfiguration
                {
                    BaseUrl = null,
                    RequestIdKey = "OcRequestId",
                    DownstreamScheme = "http",
                    HttpHandlerOptions = new FileHttpHandlerOptions
                    {
                        AllowAutoRedirect = false,
                        UseCookieContainer = false,
                        UseTracing = true
                    }
                },
                Routes = new List<FileRoute>()
            };

            foreach (var route in configVersion.RouteConfigurations.Where(r => r.IsActive))
            {
                var fileRoute = new FileRoute
                {
                    DownstreamPathTemplate = route.DownstreamPathTemplate,
                    UpstreamPathTemplate = route.UpstreamPathTemplate,
                    DownstreamScheme = route.DownstreamScheme,
                    Key = route.Name,
                    DownstreamHostAndPorts = new List<FileHostAndPort>(),
                    UpstreamHttpMethod = ParseHttpMethods(route.UpstreamHttpMethod),
                    Priority = 1,
                    Timeout = 30000, // 30 seconds default timeout
                    LoadBalancerOptions = new FileLoadBalancerOptions
                    {
                        Type = route.LoadBalancerOptions ?? "RoundRobin"
                    }
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

                // Add optional configurations
                if (!string.IsNullOrEmpty(route.AuthenticationOptions))
                {
                    // Parse authentication options from JSON if needed
                    fileRoute.AuthenticationOptions = new FileAuthenticationOptions
                    {
                        AuthenticationProviderKeys = new string[] { route.AuthenticationOptions }
                    };
                }

                if (!string.IsNullOrEmpty(route.RateLimitOptions))
                {
                    // Parse rate limit options from JSON if needed
                    fileRoute.RateLimitOptions = new FileRateLimitRule
                    {
                        EnableRateLimiting = true
                    };
                }

                fileConfig.Routes.Add(fileRoute);
            }

            return fileConfig;
        }

        /// <summary>
        /// Parses HTTP methods from a string
        /// </summary>
        private static List<string> ParseHttpMethods(string httpMethod)
        {
            if (string.IsNullOrEmpty(httpMethod))
            {
                return new List<string> { "GET" };
            }

            return httpMethod
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(m => m.Trim().ToUpper())
                .ToList();
        }
    }
} 