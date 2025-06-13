using System;
using System.Collections.Generic;

namespace OcelotGateway.Domain.Entities
{
    /// <summary>
    /// Represents a version of the Ocelot configuration with a collection of route configurations
    /// </summary>
    public class ConfigurationVersion
    {
        public Guid Id { get; private set; }
        public string Version { get; private set; }
        public string Description { get; private set; }
        public string Environment { get; private set; }
        public bool IsActive { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public DateTime? PublishedAt { get; private set; }
        public string CreatedBy { get; private set; }
        public string PublishedBy { get; private set; }
        public List<RouteConfig> RouteConfigurations { get; private set; }

        // For EF Core
        private ConfigurationVersion() { }

        public ConfigurationVersion(
            string version,
            string description,
            string environment,
            string createdBy)
        {
            Id = Guid.NewGuid();
            Version = version ?? throw new ArgumentNullException(nameof(version));
            Description = description;
            Environment = environment ?? "Development";
            IsActive = false;
            CreatedAt = DateTime.UtcNow;
            CreatedBy = createdBy ?? "System";
            RouteConfigurations = new List<RouteConfig>();
        }

        public void AddRouteConfiguration(RouteConfig routeConfig)
        {
            if (routeConfig == null)
                throw new ArgumentNullException(nameof(routeConfig));

            RouteConfigurations.Add(routeConfig);
        }

        public void RemoveRouteConfiguration(RouteConfig routeConfig)
        {
            if (routeConfig == null)
                throw new ArgumentNullException(nameof(routeConfig));

            RouteConfigurations.Remove(routeConfig);
        }

        public void Publish(string publishedBy)
        {
            IsActive = true;
            PublishedAt = DateTime.UtcNow;
            PublishedBy = publishedBy ?? "System";
        }

        public void Unpublish()
        {
            IsActive = false;
        }

        public void UpdateDescription(string description)
        {
            Description = description;
        }
    }
} 