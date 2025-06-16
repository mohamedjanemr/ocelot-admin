using Microsoft.AspNetCore.Mvc;
using OcelotGateway.Application.DTOs;
using OcelotGateway.Application.Interfaces;

namespace OcelotGateway.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ConfigurationVersionController : ControllerBase
{
    private readonly IConfigurationVersionService _configurationVersionService;

    public ConfigurationVersionController(IConfigurationVersionService configurationVersionService)
    {
        _configurationVersionService = configurationVersionService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ConfigurationVersionDto>>> GetAllVersions()
    {
        try
        {
            var versions = await _configurationVersionService.GetAllVersionsAsync();
            return Ok(versions);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "An error occurred while retrieving versions", error = ex.Message });
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ConfigurationVersionDto>> GetVersionById(Guid id)
    {
        try
        {
            var version = await _configurationVersionService.GetVersionByIdAsync(id);
            if (version == null)
            {
                return NotFound(new { message = $"Version with ID {id} not found" });
            }
            return Ok(version);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "An error occurred while retrieving the version", error = ex.Message });
        }
    }

    [HttpPost]
    public async Task<ActionResult<ConfigurationVersionDto>> CreateVersion([FromBody] CreateConfigurationVersionDto createVersionDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // For now, using a default user. In a real application, this would come from authentication
            var createdBy = "system"; // TODO: Get from authenticated user context
            var createdVersion = await _configurationVersionService.CreateVersionAsync(createVersionDto, createdBy);
            return CreatedAtAction(nameof(GetVersionById), new { id = createdVersion.Id }, createdVersion);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "An error occurred while creating the version", error = ex.Message });
        }
    }

    [HttpPost("{id}/publish")]
    public async Task<ActionResult> PublishVersion(Guid id)
    {
        try
        {
            // For now, using a default user. In a real application, this would come from authentication
            var publishedBy = "system"; // TODO: Get from authenticated user context
            var success = await _configurationVersionService.PublishVersionAsync(id, publishedBy);
            if (!success)
            {
                return NotFound(new { message = $"Version with ID {id} not found" });
            }
            return Ok(new { message = "Version published successfully" });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "An error occurred while publishing the version", error = ex.Message });
        }
    }
} 