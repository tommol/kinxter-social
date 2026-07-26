using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Kinxter.Communities.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCommunities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "communities");

            migrationBuilder.CreateTable(
                name: "communities",
                schema: "communities",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OwnerProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    Slug = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    RejectionReason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    PublishedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_communities", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "memberships",
                schema: "communities",
                columns: table => new
                {
                    CommunityId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsOwner = table.Column<bool>(type: "boolean", nullable: false),
                    JoinedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_memberships", x => new { x.CommunityId, x.ProfileId });
                    table.ForeignKey(
                        name: "FK_memberships_communities_CommunityId",
                        column: x => x.CommunityId,
                        principalSchema: "communities",
                        principalTable: "communities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_communities_Slug",
                schema: "communities",
                table: "communities",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_communities_Status",
                schema: "communities",
                table: "communities",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_memberships_ProfileId",
                schema: "communities",
                table: "memberships",
                column: "ProfileId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "memberships",
                schema: "communities");

            migrationBuilder.DropTable(
                name: "communities",
                schema: "communities");
        }
    }
}
