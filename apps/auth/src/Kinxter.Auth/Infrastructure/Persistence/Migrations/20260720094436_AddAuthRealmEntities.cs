using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kinxter.Auth.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAuthRealmEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AuthRealms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Issuer = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    PathBase = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    MfaPolicy = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    SignupEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuthRealms", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AuthClients",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RealmId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClientId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Enabled = table.Column<bool>(type: "boolean", nullable: false),
                    ClientSecretConfigured = table.Column<bool>(type: "boolean", nullable: false),
                    RedirectUris = table.Column<string[]>(type: "text[]", nullable: false),
                    PostLogoutRedirectUris = table.Column<string[]>(type: "text[]", nullable: false),
                    Scopes = table.Column<string[]>(type: "text[]", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuthClients", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AuthClients_AuthRealms_RealmId",
                        column: x => x.RealmId,
                        principalTable: "AuthRealms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuthClients_ClientId",
                table: "AuthClients",
                column: "ClientId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AuthClients_RealmId_ClientId",
                table: "AuthClients",
                columns: new[] { "RealmId", "ClientId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AuthRealms_Name",
                table: "AuthRealms",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AuthRealms_PathBase",
                table: "AuthRealms",
                column: "PathBase",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuthClients");

            migrationBuilder.DropTable(
                name: "AuthRealms");
        }
    }
}
