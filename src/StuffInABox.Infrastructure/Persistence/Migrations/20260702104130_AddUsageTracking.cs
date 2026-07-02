using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StuffInABox.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddUsageTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AiUsageYearMonth",
                table: "UserSettings",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "AiUsedThisMonth",
                table: "UserSettings",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<long>(
                name: "PhotoSizeBytes",
                table: "Items",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AiUsageYearMonth",
                table: "UserSettings");

            migrationBuilder.DropColumn(
                name: "AiUsedThisMonth",
                table: "UserSettings");

            migrationBuilder.DropColumn(
                name: "PhotoSizeBytes",
                table: "Items");
        }
    }
}
