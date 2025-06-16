using System.ComponentModel.DataAnnotations;

namespace OcelotGateway.Application.DTOs;

public class ConfigurationVersionDto
{
    public Guid Id { get; set; }
    
    [Required]
    [StringLength(100)]
    public string Version { get; set; } = string.Empty;
    
    public string? Description { get; set; }
    public string Environment { get; set; } = "Development";
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? PublishedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public string PublishedBy { get; set; } = string.Empty;
    public List<RouteConfigDto> RouteConfigurations { get; set; } = new();
}

public class CreateConfigurationVersionDto
{
    [Required]
    [StringLength(100)]
    public string Version { get; set; } = string.Empty;
    
    public string? Description { get; set; }
    public string Environment { get; set; } = "Development";
    public List<Guid> RouteIds { get; set; } = new();
}

public class ActivateConfigurationVersionDto
{
    [Required]
    public Guid VersionId { get; set; }
    
    public string? ActivationReason { get; set; }
} 