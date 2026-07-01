using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StuffInABox.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPlanCatalog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Plans",
                columns: table => new
                {
                    Tier = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    PriceSek = table.Column<int>(type: "integer", nullable: false),
                    MaxSpaces = table.Column<int>(type: "integer", nullable: false),
                    MaxItems = table.Column<int>(type: "integer", nullable: false),
                    MaxMembers = table.Column<int>(type: "integer", nullable: false),
                    AiPhotosPerMonth = table.Column<int>(type: "integer", nullable: false),
                    StorageMb = table.Column<long>(type: "bigint", nullable: false),
                    ClaudeEnrichment = table.Column<bool>(type: "boolean", nullable: false),
                    PriorityQueue = table.Column<bool>(type: "boolean", nullable: false),
                    AllThemes = table.Column<bool>(type: "boolean", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Plans", x => x.Tier);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Plans");
        }
    }
}
