using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Caching.Memory;
using Ocelot.Configuration.Repository;
using Ocelot.DependencyInjection;
using OcelotGateway.Gateway.Providers;
using OcelotGateway.Gateway.Services;
using System;

namespace OcelotGateway.Gateway.Configuration
{
    /// <summary>
    /// Extension methods for configuring Ocelot with our custom configuration provider
    /// </summary>
    public static class OcelotBuilderExtensions
    {
        /// <summary>
        /// Adds the database configuration provider to Ocelot
        /// </summary>
        /// <param name="builder">The Ocelot builder</param>
        /// <param name="environment">The environment name</param>
        /// <returns>The Ocelot builder</returns>
        public static IOcelotBuilder AddDatabaseConfiguration(this IOcelotBuilder builder, string environment = "Development")
        {
            // Add memory cache if not already added
            builder.Services.AddMemoryCache();
            
            // Add configuration cache service
            builder.Services.AddSingleton<ConfigurationCacheService>();
            
            // Add database configuration provider
            builder.Services.AddSingleton<IFileConfigurationRepository>(provider =>
            {
                var cacheService = provider.GetRequiredService<ConfigurationCacheService>();
                var logger = provider.GetRequiredService<Microsoft.Extensions.Logging.ILogger<DatabaseConfigurationProvider>>();
                return new DatabaseConfigurationProvider(provider, cacheService, logger, environment);
            });

            return builder;
        }
    }
} 