using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PaymentManager.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddEffectivePaymentSplits : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EffectivePaymentSplits",
                columns: table => new
                {
                    PaymentId = table.Column<Guid>(type: "TEXT", nullable: false),
                    EffectiveDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    PersonId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Percentage = table.Column<decimal>(type: "TEXT", precision: 5, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EffectivePaymentSplits", x => new { x.PaymentId, x.EffectiveDate, x.PersonId });
                    table.ForeignKey(
                        name: "FK_EffectivePaymentSplits_Payments_PaymentId",
                        column: x => x.PaymentId,
                        principalTable: "Payments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EffectivePaymentSplits_People_PersonId",
                        column: x => x.PersonId,
                        principalTable: "People",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EffectivePaymentSplits_PersonId",
                table: "EffectivePaymentSplits",
                column: "PersonId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EffectivePaymentSplits");
        }
    }
}
