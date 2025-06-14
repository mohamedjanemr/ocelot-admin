using System.ComponentModel.DataAnnotations;

namespace OcelotGateway.Application.DTOs;

public class RouteConfigDto
{
    public Guid Id { get; set; }
    
    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;
    
    [Required]
    [StringLength(200)]
    public string DownstreamPathTemplate { get; set; } = string.Empty;
    
    [Required]
    [StringLength(200)]
    public string UpstreamPathTemplate { get; set; } = string.Empty;
    
    [Required]
    [StringLength(50)]
    public string UpstreamHttpMethod { get; set; } = string.Empty;
    
    public List<string> UpstreamHttpMethods { get; set; } = new();
    
    [Required]
    [StringLength(50)]
    public string DownstreamHttpMethod { get; set; } = string.Empty;
    
    [Required]
    [StringLength(100)]
    public string DownstreamScheme { get; set; } = string.Empty;
    
    public bool RouteIsCaseSensitive { get; set; }
    
    [Required]
    public List<HostAndPortDto> DownstreamHostAndPorts { get; set; } = new();
    
    public string? ServiceName { get; set; }
    public string? LoadBalancerOptions { get; set; }
    public string? AuthenticationOptions { get; set; }
    public string? RateLimitOptions { get; set; }
    public string? QoSOptions { get; set; }
    public bool IsActive { get; set; } = true;
    public string Environment { get; set; } = "Development";
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public string UpdatedBy { get; set; } = string.Empty;
}

public class HostAndPortDto
{
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; }
}

public class CreateRouteConfigDto
{
    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;
    
    [Required]
    [StringLength(200)]
    public string DownstreamPathTemplate { get; set; } = string.Empty;
    
    [Required]
    [StringLength(200)]
    public string UpstreamPathTemplate { get; set; } = string.Empty;
    
    [Required]
    [StringLength(50)]
    public string UpstreamHttpMethod { get; set; } = string.Empty;
    
    [Required]
    [StringLength(100)]
    public string DownstreamScheme { get; set; } = string.Empty;
    
    [Required]
    public List<HostAndPortDto> DownstreamHostAndPorts { get; set; } = new();
    
    public string Environment { get; set; } = "Development";
    public string? ServiceName { get; set; }
    public string? LoadBalancerOptions { get; set; }
    public string? AuthenticationOptions { get; set; }
    public string? RateLimitOptions { get; set; }
    public string? QoSOptions { get; set; }
}

public class UpdateRouteConfigDto
{
    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;
    
    [Required]
    [StringLength(200)]
    public string DownstreamPathTemplate { get; set; } = string.Empty;
    
    [Required]
    [StringLength(200)]
    public string UpstreamPathTemplate { get; set; } = string.Empty;
    
    [Required]
    [StringLength(50)]
    public string UpstreamHttpMethod { get; set; } = string.Empty;
    
    [Required]
    [StringLength(100)]
    public string DownstreamScheme { get; set; } = string.Empty;
    
    [Required]
    public List<HostAndPortDto> DownstreamHostAndPorts { get; set; } = new();
    
    public string? ServiceName { get; set; }
    public string? LoadBalancerOptions { get; set; }
    public string? AuthenticationOptions { get; set; }
    public string? RateLimitOptions { get; set; }
    public string? QoSOptions { get; set; }
} 