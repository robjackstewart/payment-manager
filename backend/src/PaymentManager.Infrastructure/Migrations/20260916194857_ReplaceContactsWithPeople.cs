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
