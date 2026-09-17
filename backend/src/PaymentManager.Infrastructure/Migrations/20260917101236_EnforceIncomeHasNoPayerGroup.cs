using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PaymentManager.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class EnforceIncomeHasNoPayerGroup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Income never belongs to a payer group. Clear any legacy grouped income before the
            // constraint goes on so the migration cannot fail on pre-existing data.
            migrationBuilder.Sql(
                """
                UPDATE "Payments" SET "PayerGroupId" = NULL
                WHERE "Direction" = 'Incoming' AND "PayerGroupId" IS NOT NULL;
                """);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Payments_IncomingHasNoPayerGroup",
                table: "Payments",
                sql: "\"Direction\" <> 'Incoming' OR \"PayerGroupId\" IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Payments_IncomingHasNoPayerGroup",
                table: "Payments");
        }
    }
}
