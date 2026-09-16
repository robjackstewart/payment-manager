using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PaymentManager.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ReplaceContactsWithPeople : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PaymentSplits_Contacts_ContactId",
                table: "PaymentSplits");

            migrationBuilder.CreateTable(
                name: "People",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    IsSelf = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_People", x => x.Id);
                    table.ForeignKey(
                        name: "FK_People_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // Preserve existing contacts as people, keeping their ids so payment splits still resolve.
            migrationBuilder.Sql(
                """
                INSERT INTO "People" ("Id", "UserId", "Name", "IsSelf")
                SELECT "Id", "UserId", "Name", 0
                FROM "Contacts";
                """);

            // The owner was previously implicit (they absorbed the split remainder). Represent them
            // as a normal person with IsSelf = 1 so RemovePersonIsSelf renames them to 'Current User'.
            // Id = User.Id gives each payment's owner split a stable person id. Only created for users
            // that actually have payments, so a fresh database does not gain a spurious person.
            migrationBuilder.Sql(
                """
                INSERT INTO "People" ("Id", "UserId", "Name", "IsSelf")
                SELECT u."Id", u."Id", u."Name", 1
                FROM "Users" u
                WHERE EXISTS (SELECT 1 FROM "Payments" p WHERE p."UserId" = u."Id");
                """);

            migrationBuilder.CreateTable(
                name: "PayerGroupMembers",
                columns: table => new
                {
                    PayerGroupId = table.Column<Guid>(type: "TEXT", nullable: false),
                    PersonId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PayerGroupMembers", x => new { x.PayerGroupId, x.PersonId });
                    table.ForeignKey(
                        name: "FK_PayerGroupMembers_PayerGroups_PayerGroupId",
                        column: x => x.PayerGroupId,
                        principalTable: "PayerGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PayerGroupMembers_People_PersonId",
                        column: x => x.PersonId,
                        principalTable: "People",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // Existing payments only carry other people's shares; add the owner's remainder so every
            // payment's splits total 100. This runs before ContactId is renamed to PersonId, so it
            // targets the old column name (foreign keys are not enforced during migration).
            migrationBuilder.Sql(
                """
                INSERT INTO "PaymentSplits" ("PaymentId", "ContactId", "Percentage")
                SELECT p."Id", p."UserId", 100 - COALESCE(s."Total", 0)
                FROM "Payments" p
                LEFT JOIN (
                    SELECT "PaymentId", SUM("Percentage") AS "Total"
                    FROM "PaymentSplits"
                    GROUP BY "PaymentId"
                ) s ON s."PaymentId" = p."Id"
                WHERE 100 - COALESCE(s."Total", 0) > 0;
                """);

            migrationBuilder.DropTable(
                name: "Contacts");

            migrationBuilder.RenameColumn(
                name: "ContactId",
                table: "PaymentSplits",
                newName: "PersonId");

            migrationBuilder.RenameIndex(
                name: "IX_PaymentSplits_ContactId",
                table: "PaymentSplits",
                newName: "IX_PaymentSplits_PersonId");

            migrationBuilder.CreateIndex(
                name: "IX_PayerGroupMembers_PersonId",
                table: "PayerGroupMembers",
                column: "PersonId");

            migrationBuilder.CreateIndex(
                name: "IX_People_UserId",
                table: "People",
                column: "UserId",
                unique: true,
                filter: "\"IsSelf\" = 1");

            migrationBuilder.AddForeignKey(
                name: "FK_PaymentSplits_People_PersonId",
                table: "PaymentSplits",
                column: "PersonId",
                principalTable: "People",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Remove the owner splits added above; the owner share is implicit again in the old model.
            migrationBuilder.Sql(
                """
                DELETE FROM "PaymentSplits"
                WHERE EXISTS (
                    SELECT 1
                    FROM "Payments" p
                    WHERE p."Id" = "PaymentSplits"."PaymentId"
                      AND p."UserId" = "PaymentSplits"."PersonId"
                );
                """);

            migrationBuilder.DropForeignKey(
                name: "FK_PaymentSplits_People_PersonId",
                table: "PaymentSplits");

            migrationBuilder.DropTable(
                name: "PayerGroupMembers");

            migrationBuilder.DropTable(
                name: "People");

            migrationBuilder.RenameColumn(
                name: "PersonId",
                table: "PaymentSplits",
                newName: "ContactId");

            migrationBuilder.RenameIndex(
                name: "IX_PaymentSplits_PersonId",
                table: "PaymentSplits",
                newName: "IX_PaymentSplits_ContactId");

            migrationBuilder.CreateTable(
                name: "Contacts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    PayerGroupId = table.Column<Guid>(type: "TEXT", nullable: true),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Contacts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Contacts_PayerGroups_PayerGroupId",
                        column: x => x.PayerGroupId,
                        principalTable: "PayerGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Contacts_Users_UserId",
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
                name: "IX_Contacts_UserId",
                table: "Contacts",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_PaymentSplits_Contacts_ContactId",
                table: "PaymentSplits",
                column: "ContactId",
                principalTable: "Contacts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
