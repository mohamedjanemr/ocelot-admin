using System;

namespace OcelotGateway.Domain.ValueObjects
{
    /// <summary>
    /// Value object representing a host and port combination for downstream services
    /// </summary>
    public class HostAndPort
    {
        public string Host { get; }
        public int Port { get; }

        // For EF Core
        private HostAndPort() { }

        public HostAndPort(string host, int port)
        {
            if (string.IsNullOrWhiteSpace(host))
                throw new ArgumentException("Host cannot be null or empty", nameof(host));

            if (port <= 0 || port > 65535)
                throw new ArgumentOutOfRangeException(nameof(port), "Port must be between 1 and 65535");

            Host = host;
            Port = port;
        }

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
                return false;

            var other = (HostAndPort)obj;
            return Host == other.Host && Port == other.Port;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Host, Port);
        }

        public override string ToString()
        {
            return $"{Host}:{Port}";
        }
    }
} 