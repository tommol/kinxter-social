using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kinxter.SocialGraph.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialSocialGraph : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "social_graph");

            migrationBuilder.CreateTable(
                name: "follows",
                schema: "social_graph",
                columns: table => new
                {
                    FollowerProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    FollowedProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    AcceptedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_follows", x => new { x.FollowerProfileId, x.FollowedProfileId });
                });

            migrationBuilder.CreateIndex(
                name: "IX_follows_FollowedProfileId_Status",
                schema: "social_graph",
                table: "follows",
                columns: new[] { "FollowedProfileId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "follows",
                schema: "social_graph");
        }
    }
}
