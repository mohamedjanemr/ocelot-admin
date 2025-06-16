using OcelotGateway.Application.DTOs;

namespace OcelotGateway.Application.Interfaces;

public interface IConfigurationVersionService
{
    Task<IEnumerable<ConfigurationVersionDto>> GetAllVersionsAsync();
    Task<ConfigurationVersionDto?> GetVersionByIdAsync(Guid id);
    Task<ConfigurationVersionDto?> GetByVersionAsync(string version, string environment);
    Task<ConfigurationVersionDto?> GetActiveConfigurationAsync(string environment);
    Task<IEnumerable<ConfigurationVersionDto>> GetByEnvironmentAsync(string environment);
    Task<ConfigurationVersionDto> CreateVersionAsync(CreateConfigurationVersionDto createDto, string createdBy);
    Task<bool> PublishVersionAsync(Guid versionId, string publishedBy);
    Task<bool> UnpublishVersionAsync(Guid versionId);
    Task<bool> DeleteVersionAsync(Guid id);
    Task<string> GenerateOcelotConfigurationAsync(Guid versionId);
    Task<bool> ValidateConfigurationAsync(Guid versionId);
} 