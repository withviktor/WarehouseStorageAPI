using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WarehouseStorageAPI.Migrations
{
    /// <inheritdoc />
    public partial class RemoveSeeding : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "StorageItems",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "StorageItems",
                columns: new[] { "Id", "Category", "CreatedAt", "Description", "IsActive", "LastUpdated", "LedZone", "Location", "Name", "Price", "Quantity", "SKU" },
                values: new object[] { 1, "Electronics", new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Sample warehouse item", true, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 1, "A1-01", "Sample Item", 29.99m, 100, "SAMPLE001" });

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "CreatedAt", "FirstName", "IsActive", "LastLogin", "LastName", "PasswordHash", "Role", "UserId" },
                values: new object[] { 1, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Admin", true, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "User", "ncRvoNQV64L8QfURatobD/EsZzu9czq0DB9FCvdeRmo=", 1, "1000001" });
        }
    }
}
