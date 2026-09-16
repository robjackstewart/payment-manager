using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PaymentManager.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentPayerGroup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "PayerGroupId",
                table: "Payments",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Payments_PayerGroupId",
                table: "Payments",
                column: "PayerGroupId");

            migrationBuilder.AddForeignKey(
                name: "FK_Payments_PayerGroups_PayerGroupId",
                table: "Payments",
                column: "PayerGroupId",
                principalTable: "PayerGroups",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Payments_PayerGroups_PayerGroupId",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_Payments_PayerGroupId",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "PayerGroupId",
                table: "Payments");
        }
    }
}
