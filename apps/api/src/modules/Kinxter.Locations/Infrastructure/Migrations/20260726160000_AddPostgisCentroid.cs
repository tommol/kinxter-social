using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kinxter.Locations.Infrastructure.Migrations;

[DbContext(typeof(LocationsDbContext))]
[Migration("20260726160000_AddPostgisCentroid")]
public sealed class AddPostgisCentroid : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS postgis;");
        migrationBuilder.Sql("""
            ALTER TABLE locations.places
            ADD COLUMN centroid geography(Point, 4326)
            GENERATED ALWAYS AS (
                ST_SetSRID(ST_MakePoint("Longitude", "Latitude"), 4326)::geography
            ) STORED;
            """);
        migrationBuilder.Sql("CREATE INDEX \"IX_places_centroid\" ON locations.places USING GIST (centroid);");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("DROP INDEX IF EXISTS locations.\"IX_places_centroid\";");
        migrationBuilder.Sql("ALTER TABLE locations.places DROP COLUMN IF EXISTS centroid;");
    }
}
