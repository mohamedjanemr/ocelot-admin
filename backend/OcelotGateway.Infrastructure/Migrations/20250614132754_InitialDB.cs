using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OcelotGateway.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialDB : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ConfigurationVersions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Version = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    Environment = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    PublishedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedBy = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    PublishedBy = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConfigurationVersions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RouteConfigs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    DownstreamPathTemplate = table.Column<string>(type: "TEXT", nullable: false),
                    UpstreamPathTemplate = table.Column<string>(type: "TEXT", nullable: false),
                    UpstreamHttpMethod = table.Column<string>(type: "TEXT", nullable: false),
                    UpstreamHttpMethods = table.Column<string>(type: "TEXT", nullable: false),
                    DownstreamHttpMethod = table.Column<string>(type: "TEXT", nullable: false),
                    DownstreamScheme = table.Column<string>(type: "TEXT", nullable: false),
                    RouteIsCaseSensitive = table.Column<bool>(type: "INTEGER", nullable: false),
                    DownstreamHostAndPorts = table.Column<string>(type: "TEXT", nullable: false),
                    ServiceName = table.Column<string>(type: "TEXT", nullable: false),
                    LoadBalancerOptions = table.Column<string>(type: "TEXT", nullable: false),
                    AuthenticationOptions = table.Column<string>(type: "TEXT", nullable: false),
                    RateLimitOptions = table.Column<string>(type: "TEXT", nullable: false),
                    QoSOptions = table.Column<string>(type: "TEXT", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    Environment = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedBy = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    UpdatedBy = table.Column<string>(type: "TEXT", nullable: false),
                    ConfigurationVersionId = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RouteConfigs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RouteConfigs_ConfigurationVersions_ConfigurationVersionId",
                        column: x => x.ConfigurationVersionId,
                        principalTable: "ConfigurationVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RouteConfigs_ConfigurationVersionId",
                table: "RouteConfigs",
                column: "ConfigurationVersionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RouteConfigs");

            migrationBuilder.DropTable(
                name: "ConfigurationVersions");
        }
    }
}
