using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using OcelotGateway.Application.DTOs;
using OcelotGateway.Application.Services;
using OcelotGateway.Domain.Entities;
using OcelotGateway.Domain.Interfaces;
using Xunit;

namespace OcelotGateway.Tests.Services;

public class RouteConfigServiceTests
{
    private readonly Mock<IRouteConfigRepository> _mockRepository;
    private readonly RouteConfigService _service;

    public RouteConfigServiceTests()
    {
        _mockRepository = new Mock<IRouteConfigRepository>();
        _service = new RouteConfigService(_mockRepository.Object);
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnAllRouteConfigs()
    {
        // Arrange
        var routeConfigs = new List<RouteConfig>
        {
            new RouteConfig
            {
                Id = 1,
                Name = "Test Route 1",
                DownstreamPath = "/api/test1",
                UpstreamPath = "/test1",
                UpstreamHttpMethods = new List<string> { "GET" },
                DownstreamScheme = "http",
                DownstreamHost = "localhost",
                DownstreamPort = 5001,
                IsActive = true
            },
            new RouteConfig
            {
                Id = 2,
                Name = "Test Route 2",
                DownstreamPath = "/api/test2",
                UpstreamPath = "/test2",
                UpstreamHttpMethods = new List<string> { "POST" },
                DownstreamScheme = "http",
                DownstreamHost = "localhost",
                DownstreamPort = 5002,
                IsActive = false
            }
        };

        _mockRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(routeConfigs);

        // Act
        var result = await _service.GetAllAsync();

        // Assert
        result.Should().HaveCount(2);
        result.First().Name.Should().Be("Test Route 1");
        result.Last().Name.Should().Be("Test Route 2");
        _mockRepository.Verify(r => r.GetAllAsync(), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnRouteConfig_WhenExists()
    {
        // Arrange
        var routeConfig = new RouteConfig
        {
            Id = 1,
            Name = "Test Route",
            DownstreamPath = "/api/test",
            UpstreamPath = "/test",
            UpstreamHttpMethods = new List<string> { "GET" },
            DownstreamScheme = "http",
            DownstreamHost = "localhost",
            DownstreamPort = 5001,
            IsActive = true
        };

        _mockRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(routeConfig);

        // Act
        var result = await _service.GetByIdAsync(1);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("Test Route");
        result.IsActive.Should().BeTrue();
        _mockRepository.Verify(r => r.GetByIdAsync(1), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnNull_WhenNotExists()
    {
        // Arrange
        _mockRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync((RouteConfig?)null);

        // Act
        var result = await _service.GetByIdAsync(1);

        // Assert
        result.Should().BeNull();
        _mockRepository.Verify(r => r.GetByIdAsync(1), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_ShouldCreateAndReturnRouteConfig()
    {
        // Arrange
        var createDto = new RouteConfigDto
        {
            Name = "New Route",
            DownstreamPath = "/api/new",
            UpstreamPath = "/new",
            UpstreamHttpMethods = new List<string> { "GET", "POST" },
            DownstreamScheme = "https",
            DownstreamHost = "api.example.com",
            DownstreamPort = 443,
            IsActive = true
        };

        var createdRoute = new RouteConfig
        {
            Id = 1,
            Name = createDto.Name,
            DownstreamPath = createDto.DownstreamPath,
            UpstreamPath = createDto.UpstreamPath,
            UpstreamHttpMethods = createDto.UpstreamHttpMethods,
            DownstreamScheme = createDto.DownstreamScheme,
            DownstreamHost = createDto.DownstreamHost,
            DownstreamPort = createDto.DownstreamPort,
            IsActive = createDto.IsActive,
            CreatedAt = DateTime.UtcNow
        };

        _mockRepository.Setup(r => r.CreateAsync(It.IsAny<RouteConfig>())).ReturnsAsync(createdRoute);

        // Act
        var result = await _service.CreateAsync(createDto);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("New Route");
        result.DownstreamHost.Should().Be("api.example.com");
        result.IsActive.Should().BeTrue();
        _mockRepository.Verify(r => r.CreateAsync(It.IsAny<RouteConfig>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_ShouldUpdateAndReturnRouteConfig_WhenExists()
    {
        // Arrange
        var updateDto = new RouteConfigDto
        {
            Id = 1,
            Name = "Updated Route",
            DownstreamPath = "/api/updated",
            UpstreamPath = "/updated",
            UpstreamHttpMethods = new List<string> { "PUT" },
            DownstreamScheme = "https",
            DownstreamHost = "updated.example.com",
            DownstreamPort = 443,
            IsActive = false
        };

        var existingRoute = new RouteConfig
        {
            Id = 1,
            Name = "Original Route",
            DownstreamPath = "/api/original",
            UpstreamPath = "/original",
            UpstreamHttpMethods = new List<string> { "GET" },
            DownstreamScheme = "http",
            DownstreamHost = "original.example.com",
            DownstreamPort = 80,
            IsActive = true,
            CreatedAt = DateTime.UtcNow.AddDays(-1)
        };

        var updatedRoute = new RouteConfig
        {
            Id = updateDto.Id,
            Name = updateDto.Name,
            DownstreamPath = updateDto.DownstreamPath,
            UpstreamPath = updateDto.UpstreamPath,
            UpstreamHttpMethods = updateDto.UpstreamHttpMethods,
            DownstreamScheme = updateDto.DownstreamScheme,
            DownstreamHost = updateDto.DownstreamHost,
            DownstreamPort = updateDto.DownstreamPort,
            IsActive = updateDto.IsActive,
            CreatedAt = existingRoute.CreatedAt,
            UpdatedAt = DateTime.UtcNow
        };

        _mockRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(existingRoute);
        _mockRepository.Setup(r => r.UpdateAsync(It.IsAny<RouteConfig>())).ReturnsAsync(updatedRoute);

        // Act
        var result = await _service.UpdateAsync(updateDto);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("Updated Route");
        result.DownstreamHost.Should().Be("updated.example.com");
        result.IsActive.Should().BeFalse();
        _mockRepository.Verify(r => r.GetByIdAsync(1), Times.Once);
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<RouteConfig>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_ShouldReturnNull_WhenNotExists()
    {
        // Arrange
        var updateDto = new RouteConfigDto { Id = 999, Name = "Non-existent Route" };

        _mockRepository.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((RouteConfig?)null);

        // Act
        var result = await _service.UpdateAsync(updateDto);

        // Assert
        result.Should().BeNull();
        _mockRepository.Verify(r => r.GetByIdAsync(999), Times.Once);
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<RouteConfig>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_ShouldReturnTrue_WhenExists()
    {
        // Arrange
        _mockRepository.Setup(r => r.DeleteAsync(1)).ReturnsAsync(true);

        // Act
        var result = await _service.DeleteAsync(1);

        // Assert
        result.Should().BeTrue();
        _mockRepository.Verify(r => r.DeleteAsync(1), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_ShouldReturnFalse_WhenNotExists()
    {
        // Arrange
        _mockRepository.Setup(r => r.DeleteAsync(999)).ReturnsAsync(false);

        // Act
        var result = await _service.DeleteAsync(999);

        // Assert
        result.Should().BeFalse();
        _mockRepository.Verify(r => r.DeleteAsync(999), Times.Once);
    }
} 