using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PaymentManager.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPayerGroupsAndPaymentDirection : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Direction",
                table: "Payments",
                type: "TEXT",
                nullable: false,
                defaultValue: "Outgoing");

            migrationBuilder.AddColumn<Guid>(
                name: "PayerGroupId",
                table: "Contacts",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PayerGroups",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PayerGroups", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PayerGroups_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Contacts_PayerGroupId",
                table: "Contacts",
                column: "PayerGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_PayerGroups_UserId",
                table: "PayerGroups",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Contacts_PayerGroups_PayerGroupId",
                table: "Contacts",
                column: "PayerGroupId",
                principalTable: "PayerGroups",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Contacts_PayerGroups_PayerGroupId",
                table: "Contacts");

            migrationBuilder.DropTable(
                name: "PayerGroups");

            migrationBuilder.DropIndex(
                name: "IX_Contacts_PayerGroupId",
                table: "Contacts");

            migrationBuilder.DropColumn(
                name: "Direction",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "PayerGroupId",
                table: "Contacts");
        }
    }
}
