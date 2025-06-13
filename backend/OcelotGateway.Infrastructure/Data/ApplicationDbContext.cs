using Microsoft.EntityFrameworkCore;
using OcelotGateway.Domain.Entities;
using OcelotGateway.Domain.ValueObjects;
using System.Text.Json;

namespace OcelotGateway.Infrastructure.Data
{
    public class ApplicationDbContext : DbContext
    {
        public DbSet<RouteConfig> RouteConfigs { get; set; }
        public DbSet<ConfigurationVersion> ConfigurationVersions { get; set; }

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // RouteConfig configuration
            modelBuilder.Entity<RouteConfig>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.DownstreamPathTemplate).IsRequired();
                entity.Property(e => e.UpstreamPathTemplate).IsRequired();
                entity.Property(e => e.Environment).IsRequired().HasMaxLength(50);
                entity.Property(e => e.CreatedAt).IsRequired();
                entity.Property(e => e.CreatedBy).IsRequired().HasMaxLength(100);

                // Store UpstreamHttpMethods as JSON
                entity.Property(e => e.UpstreamHttpMethods)
                    .HasConversion(
                        v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null),
                        v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions)null));

                // Store DownstreamHostAndPorts as JSON
                entity.Property(e => e.DownstreamHostAndPorts)
                    .HasConversion(
                        v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null),
                        v => JsonSerializer.Deserialize<List<HostAndPort>>(v, (JsonSerializerOptions)null));
            });

            // ConfigurationVersion configuration
            modelBuilder.Entity<ConfigurationVersion>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Version).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Environment).IsRequired().HasMaxLength(50);
                entity.Property(e => e.CreatedAt).IsRequired();
                entity.Property(e => e.CreatedBy).IsRequired().HasMaxLength(100);

                // Define relationship with RouteConfig
                entity.HasMany(e => e.RouteConfigurations)
                    .WithOne()
                    .HasForeignKey("ConfigurationVersionId")
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
} 