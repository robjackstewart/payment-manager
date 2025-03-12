using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PaymentManager.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDefaultUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "Name" },
                values: new object[] { new Guid("ae25b45e-63af-4b89-a8e8-2bb3e142f06d"), "Default User" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: new Guid("ae25b45e-63af-4b89-a8e8-2bb3e142f06d"));
        }
    }
}
