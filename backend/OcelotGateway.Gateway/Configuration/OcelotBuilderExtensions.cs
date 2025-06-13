using Microsoft.Extensions.DependencyInjection;
using Ocelot.Configuration.Repository;
using Ocelot.DependencyInjection;
using OcelotGateway.Gateway.Providers;
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
            builder.Services.AddSingleton<IFileConfigurationRepository>(provider =>
                new DatabaseConfigurationProvider(provider, environment));

            return builder;
        }
    }
} 