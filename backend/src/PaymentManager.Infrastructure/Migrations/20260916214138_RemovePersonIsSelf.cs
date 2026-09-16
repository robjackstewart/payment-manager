using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PaymentManager.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemovePersonIsSelf : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // The owner is no longer special, but keep the existing row and give it the neutral
            // name — unless a "Current User" person already exists.
            migrationBuilder.Sql(
                """
                UPDATE "People" SET "Name" = 'Current User'
                WHERE "IsSelf" = 1
                  AND NOT EXISTS (SELECT 1 FROM "People" WHERE "Name" = 'Current User');
                """);

            migrationBuilder.DropIndex(
                name: "IX_People_UserId",
                table: "People");

            migrationBuilder.DropColumn(
                name: "IsSelf",
                table: "People");

            migrationBuilder.CreateIndex(
                name: "IX_People_UserId",
                table: "People",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_People_UserId",
                table: "People");

            migrationBuilder.AddColumn<bool>(
                name: "IsSelf",
                table: "People",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_People_UserId",
                table: "People",
                column: "UserId",
                unique: true,
                filter: "\"IsSelf\" = 1");
        }
    }
}
