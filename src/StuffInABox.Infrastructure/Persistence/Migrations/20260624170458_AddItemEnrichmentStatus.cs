using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StuffInABox.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddItemEnrichmentStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "EnrichmentStatus",
                table: "Items",
                type: "integer",
                nullable: false,
                defaultValue: 1);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EnrichmentStatus",
                table: "Items");
        }
    }
}
