using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kinxter.Locations.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialLocations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "locations");

            migrationBuilder.CreateTable(
                name: "places",
                schema: "locations",
                columns: table => new
                {
                    GeoNameId = table.Column<long>(type: "bigint", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    AdminRegion = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    CountryCode = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    Latitude = table.Column<double>(type: "double precision", nullable: false),
                    Longitude = table.Column<double>(type: "double precision", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_places", x => x.GeoNameId);
                });

            migrationBuilder.CreateTable(
                name: "entity_locations",
                schema: "locations",
                columns: table => new
                {
                    EntityType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    EntityId = table.Column<Guid>(type: "uuid", nullable: false),
                    PlaceId = table.Column<long>(type: "bigint", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_entity_locations", x => new { x.EntityType, x.EntityId });
                    table.ForeignKey(
                        name: "FK_entity_locations_places_PlaceId",
                        column: x => x.PlaceId,
                        principalSchema: "locations",
                        principalTable: "places",
                        principalColumn: "GeoNameId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_entity_locations_PlaceId",
                schema: "locations",
                table: "entity_locations",
                column: "PlaceId");

            migrationBuilder.CreateIndex(
                name: "IX_places_CountryCode_AdminRegion_Name",
                schema: "locations",
                table: "places",
                columns: new[] { "CountryCode", "AdminRegion", "Name" });

            migrationBuilder.CreateIndex(
                name: "IX_places_Name",
                schema: "locations",
                table: "places",
                column: "Name");

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "entity_locations",
                schema: "locations");

            migrationBuilder.DropTable(
                name: "places",
                schema: "locations");
        }
    }
}
