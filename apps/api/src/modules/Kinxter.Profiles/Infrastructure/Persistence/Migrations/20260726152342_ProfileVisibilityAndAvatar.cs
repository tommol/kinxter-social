using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kinxter.Profiles.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ProfileVisibilityAndAvatar : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "AvatarAssetId",
                schema: "profiles",
                table: "profiles",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Visibility",
                schema: "profiles",
                table: "profiles",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.Sql(
                "UPDATE profiles.profiles SET \"Visibility\" = 'Private' WHERE \"OnboardingCompletedAt\" IS NOT NULL;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AvatarAssetId",
                schema: "profiles",
                table: "profiles");

            migrationBuilder.DropColumn(
                name: "Visibility",
                schema: "profiles",
                table: "profiles");
        }
    }
}
