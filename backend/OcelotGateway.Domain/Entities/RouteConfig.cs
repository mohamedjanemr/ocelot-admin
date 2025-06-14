using System;
using System.Collections.Generic;
using OcelotGateway.Domain.ValueObjects;

namespace OcelotGateway.Domain.Entities
{
    /// <summary>
    /// Represents a configuration for an API route in Ocelot Gateway
    /// </summary>
    public class RouteConfig
    {
        public Guid Id { get; private set; }
        public string Name { get; private set; }
        public string DownstreamPathTemplate { get; private set; }
        public string UpstreamPathTemplate { get; private set; }
        public string UpstreamHttpMethod { get; private set; }
        public List<string> UpstreamHttpMethods { get; private set; }
        public string DownstreamHttpMethod { get; private set; }
        public string DownstreamScheme { get; private set; }
        public bool RouteIsCaseSensitive { get; private set; }
        public List<HostAndPort> DownstreamHostAndPorts { get; private set; }
        public string ServiceName { get; private set; }
        public string LoadBalancerOptions { get; private set; }
        public string AuthenticationOptions { get; private set; }
        public string RateLimitOptions { get; private set; }
        public string QoSOptions { get; private set; }
        public bool IsActive { get; private set; }
        public string Environment { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public DateTime? UpdatedAt { get; private set; }
        public string CreatedBy { get; private set; }
        public string UpdatedBy { get; private set; }

        // For EF Core
        private RouteConfig() { }

        public RouteConfig(
            string name,
            string downstreamPathTemplate,
            string upstreamPathTemplate,
            string upstreamHttpMethod,
            string downstreamScheme,
            List<HostAndPort> downstreamHostAndPorts,
            string environment,
            string createdBy)
        {
            Id = Guid.NewGuid();
            Name = name ?? throw new ArgumentNullException(nameof(name));
            DownstreamPathTemplate = downstreamPathTemplate ?? throw new ArgumentNullException(nameof(downstreamPathTemplate));
            UpstreamPathTemplate = upstreamPathTemplate ?? throw new ArgumentNullException(nameof(upstreamPathTemplate));
            UpstreamHttpMethod = upstreamHttpMethod ?? "GET";
            UpstreamHttpMethods = !string.IsNullOrEmpty(upstreamHttpMethod) ? new List<string> { upstreamHttpMethod } : new List<string> { "GET" };
            DownstreamHttpMethod = upstreamHttpMethod ?? "GET";
            DownstreamScheme = downstreamScheme ?? "http";
            RouteIsCaseSensitive = false;
            DownstreamHostAndPorts = downstreamHostAndPorts ?? throw new ArgumentNullException(nameof(downstreamHostAndPorts));
            Environment = environment ?? "Development";
            IsActive = true;
            CreatedAt = DateTime.UtcNow;
            CreatedBy = createdBy ?? "System";
            ServiceName = string.Empty;
            LoadBalancerOptions = string.Empty;
            AuthenticationOptions = string.Empty;
            RateLimitOptions = string.Empty;
            QoSOptions = string.Empty;
            UpdatedBy = createdBy ?? "System";
        }

        public void Update(
            string name,
            string downstreamPathTemplate,
            string upstreamPathTemplate,
            string upstreamHttpMethod,
            string downstreamScheme,
            List<HostAndPort> downstreamHostAndPorts,
            string updatedBy)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            DownstreamPathTemplate = downstreamPathTemplate ?? throw new ArgumentNullException(nameof(downstreamPathTemplate));
            UpstreamPathTemplate = upstreamPathTemplate ?? throw new ArgumentNullException(nameof(upstreamPathTemplate));
            UpstreamHttpMethod = upstreamHttpMethod;
            UpstreamHttpMethods = upstreamHttpMethod != null ? new List<string> { upstreamHttpMethod } : UpstreamHttpMethods;
            DownstreamScheme = downstreamScheme ?? DownstreamScheme;
            DownstreamHostAndPorts = downstreamHostAndPorts ?? DownstreamHostAndPorts;
            UpdatedAt = DateTime.UtcNow;
            UpdatedBy = updatedBy ?? "System";
        }

        public void SetServiceName(string serviceName)
        {
            ServiceName = serviceName;
        }

        public void SetLoadBalancerOptions(string loadBalancerOptions)
        {
            LoadBalancerOptions = loadBalancerOptions;
        }

        public void SetAuthenticationOptions(string authenticationOptions)
        {
            AuthenticationOptions = authenticationOptions;
        }

        public void SetRateLimitOptions(string rateLimitOptions)
        {
            RateLimitOptions = rateLimitOptions;
        }

        public void SetQoSOptions(string qosOptions)
        {
            QoSOptions = qosOptions;
        }

        public void Activate()
        {
            IsActive = true;
        }

        public void Deactivate()
        {
            IsActive = false;
        }
    }
} 