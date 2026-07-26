using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kinxter.Tags.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialTags : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "tags");

            migrationBuilder.CreateTable(
                name: "kink_tags",
                schema: "tags",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Slug = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Category = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    NamePl = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    NameEn = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    DescriptionPl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    DescriptionEn = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_kink_tags", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "entity_tag_assignments",
                schema: "tags",
                columns: table => new
                {
                    EntityType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    EntityId = table.Column<Guid>(type: "uuid", nullable: false),
                    TagId = table.Column<Guid>(type: "uuid", nullable: false),
                    AssignedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_entity_tag_assignments", x => new { x.EntityType, x.EntityId, x.TagId });
                    table.ForeignKey(
                        name: "FK_entity_tag_assignments_kink_tags_TagId",
                        column: x => x.TagId,
                        principalSchema: "tags",
                        principalTable: "kink_tags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_entity_tag_assignments_EntityType_EntityId",
                schema: "tags",
                table: "entity_tag_assignments",
                columns: new[] { "EntityType", "EntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_entity_tag_assignments_TagId",
                schema: "tags",
                table: "entity_tag_assignments",
                column: "TagId");

            migrationBuilder.CreateIndex(
                name: "IX_kink_tags_IsActive_SortOrder",
                schema: "tags",
                table: "kink_tags",
                columns: new[] { "IsActive", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_kink_tags_Slug",
                schema: "tags",
                table: "kink_tags",
                column: "Slug",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "entity_tag_assignments",
                schema: "tags");

            migrationBuilder.DropTable(
                name: "kink_tags",
                schema: "tags");
        }
    }
}
