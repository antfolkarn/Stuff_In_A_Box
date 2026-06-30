using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StuffInABox.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDisabledAtAndPlanTier : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PlanTier",
                table: "UserSettings",
                type: "character varying(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "free");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DisabledAt",
                table: "UserIdentities",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PlanTier",
                table: "UserSettings");

            migrationBuilder.DropColumn(
                name: "DisabledAt",
                table: "UserIdentities");
        }
    }
}
