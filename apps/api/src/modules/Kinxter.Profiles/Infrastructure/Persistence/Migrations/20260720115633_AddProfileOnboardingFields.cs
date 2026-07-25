using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kinxter.Profiles.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddProfileOnboardingFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Bio",
                schema: "profiles",
                table: "profiles",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "OnboardingCompletedAt",
                schema: "profiles",
                table: "profiles",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProfilePictureUrl",
                schema: "profiles",
                table: "profiles",
                type: "character varying(2048)",
                maxLength: 2048,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Bio",
                schema: "profiles",
                table: "profiles");

            migrationBuilder.DropColumn(
                name: "OnboardingCompletedAt",
                schema: "profiles",
                table: "profiles");

            migrationBuilder.DropColumn(
                name: "ProfilePictureUrl",
                schema: "profiles",
                table: "profiles");
        }
    }
}
