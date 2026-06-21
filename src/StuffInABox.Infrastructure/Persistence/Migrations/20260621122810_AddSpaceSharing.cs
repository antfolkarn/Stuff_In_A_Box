using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StuffInABox.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSpaceSharing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SpaceInvites",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    SpaceId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Token = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    CreatedBy = table.Column<Guid>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    RevokedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SpaceInvites", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SpaceMemberships",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    SpaceId = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SpaceMemberships", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SpaceInvites_SpaceId",
                table: "SpaceInvites",
                column: "SpaceId");

            migrationBuilder.CreateIndex(
                name: "IX_SpaceInvites_Token",
                table: "SpaceInvites",
                column: "Token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SpaceMemberships_SpaceId_UserId",
                table: "SpaceMemberships",
                columns: new[] { "SpaceId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SpaceMemberships_UserId",
                table: "SpaceMemberships",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SpaceInvites");

            migrationBuilder.DropTable(
                name: "SpaceMemberships");
        }
    }
}
