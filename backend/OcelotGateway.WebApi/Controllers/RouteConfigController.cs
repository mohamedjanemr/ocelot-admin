using Microsoft.AspNetCore.Mvc;
using OcelotGateway.Application.DTOs;
using OcelotGateway.Application.Interfaces;

namespace OcelotGateway.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RouteConfigController : ControllerBase
{
    private readonly IRouteConfigService _routeConfigService;

    public RouteConfigController(IRouteConfigService routeConfigService)
    {
        _routeConfigService = routeConfigService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<RouteConfigDto>>> GetAllRoutes()
    {
        try
        {
            var routes = await _routeConfigService.GetAllRoutesAsync();
            return Ok(routes);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "An error occurred while retrieving routes", error = ex.Message });
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<RouteConfigDto>> GetRouteById(Guid id)
    {
        try
        {
            var route = await _routeConfigService.GetRouteByIdAsync(id);
            if (route == null)
            {
                return NotFound(new { message = $"Route with ID {id} not found" });
            }
            return Ok(route);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "An error occurred while retrieving the route", error = ex.Message });
        }
    }

    [HttpPost]
    public async Task<ActionResult<RouteConfigDto>> CreateRoute([FromBody] CreateRouteConfigDto createRouteDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // For now, using a default user. In a real application, this would come from authentication
            var createdBy = "system"; // TODO: Get from authenticated user context
            var createdRoute = await _routeConfigService.CreateRouteAsync(createRouteDto, createdBy);
            return CreatedAtAction(nameof(GetRouteById), new { id = createdRoute.Id }, createdRoute);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "An error occurred while creating the route", error = ex.Message });
        }
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<RouteConfigDto>> UpdateRoute(Guid id, [FromBody] UpdateRouteConfigDto updateRouteDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // For now, using a default user. In a real application, this would come from authentication
            var updatedBy = "system"; // TODO: Get from authenticated user context
            var updatedRoute = await _routeConfigService.UpdateRouteAsync(id, updateRouteDto, updatedBy);
            if (updatedRoute == null)
            {
                return NotFound(new { message = $"Route with ID {id} not found" });
            }
            return Ok(updatedRoute);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "An error occurred while updating the route", error = ex.Message });
        }
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteRoute(Guid id)
    {
        try
        {
            var success = await _routeConfigService.DeleteRouteAsync(id);
            if (!success)
            {
                return NotFound(new { message = $"Route with ID {id} not found" });
            }
            return NoContent();
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "An error occurred while deleting the route", error = ex.Message });
        }
    }
} 