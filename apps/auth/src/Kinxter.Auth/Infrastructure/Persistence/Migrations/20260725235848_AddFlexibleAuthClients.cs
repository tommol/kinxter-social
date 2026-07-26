using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kinxter.Auth.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddFlexibleAuthClients : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ClientType",
                table: "AuthClients",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "Confidential");

            migrationBuilder.AddColumn<string[]>(
                name: "GrantTypes",
                table: "AuthClients",
                type: "text[]",
                nullable: false,
                defaultValue: new[] { "authorization_code", "refresh_token" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ClientType",
                table: "AuthClients");

            migrationBuilder.DropColumn(
                name: "GrantTypes",
                table: "AuthClients");
        }
    }
}
