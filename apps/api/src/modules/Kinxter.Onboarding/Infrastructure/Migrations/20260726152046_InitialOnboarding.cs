using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kinxter.Onboarding.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialOnboarding : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "onboarding");

            migrationBuilder.CreateTable(
                name: "progress",
                schema: "onboarding",
                columns: table => new
                {
                    AccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    InterestsStatus = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    RecommendationsStatus = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_progress", x => x.AccountId);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "progress",
                schema: "onboarding");
        }
    }
}
