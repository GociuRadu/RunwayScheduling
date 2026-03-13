using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Api.DataBase.Migrations
{
    /// <inheritdoc />
    public partial class SeedUsers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "users",
                columns: new[] { "Id", "CreatedAtUtc", "Email", "PasswordHash", "Username" },
                values: new object[,]
                {
                    { new Guid("11111111-1111-1111-1111-111111111111"), new DateTime(2026, 3, 9, 2, 0, 0, 0, DateTimeKind.Utc), "admin1@gmail.com", "Admin1234", "admin1" },
                    { new Guid("22222222-2222-2222-2222-222222222222"), new DateTime(2026, 3, 9, 3, 0, 0, 0, DateTimeKind.Utc), "admin2@gmail.com", "Admin1234", "admin2" },
                    { new Guid("33333333-3333-3333-3333-333333333333"), new DateTime(2026, 3, 9, 4, 0, 0, 0, DateTimeKind.Utc), "admin3@gmail.com", "Admin1234", "admin3" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "users",
                keyColumn: "Id",
                keyValue: new Guid("11111111-1111-1111-1111-111111111111"));

            migrationBuilder.DeleteData(
                table: "users",
                keyColumn: "Id",
                keyValue: new Guid("22222222-2222-2222-2222-222222222222"));

            migrationBuilder.DeleteData(
                table: "users",
                keyColumn: "Id",
                keyValue: new Guid("33333333-3333-3333-3333-333333333333"));
        }
    }
}
