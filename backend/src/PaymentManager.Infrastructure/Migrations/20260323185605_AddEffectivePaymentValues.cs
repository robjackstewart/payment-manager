using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PaymentManager.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddEffectivePaymentValues : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Amount",
                table: "Payments",
                newName: "InitialAmount");

            migrationBuilder.CreateTable(
                name: "EffectivePaymentValues",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    PaymentId = table.Column<Guid>(type: "TEXT", nullable: false),
                    EffectiveDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    Amount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EffectivePaymentValues", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EffectivePaymentValues_Payments_PaymentId",
                        column: x => x.PaymentId,
                        principalTable: "Payments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EffectivePaymentValues_PaymentId_EffectiveDate",
                table: "EffectivePaymentValues",
                columns: new[] { "PaymentId", "EffectiveDate" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EffectivePaymentValues");

            migrationBuilder.RenameColumn(
                name: "InitialAmount",
                table: "Payments",
                newName: "Amount");
        }
    }
}
