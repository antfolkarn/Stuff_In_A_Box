using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StuffInABox.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddUserIdentityPersonLink : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "UserId",
                table: "UserIdentities",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            // Existing accounts are their own person: seed UserId from the (old) primary key.
            migrationBuilder.Sql("UPDATE \"UserIdentities\" SET \"UserId\" = \"InternalUserId\"");

            migrationBuilder.CreateIndex(
                name: "IX_UserIdentities_UserId",
                table: "UserIdentities",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UserIdentities_UserId",
                table: "UserIdentities");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "UserIdentities");
        }
    }
}
