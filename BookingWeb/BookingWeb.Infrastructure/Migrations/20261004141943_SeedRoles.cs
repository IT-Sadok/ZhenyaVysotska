using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace BookingWeb.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SeedRoles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DefaultPersona",
                table: "AspNetUsers");

            migrationBuilder.InsertData(
                table: "AspNetRoles",
                columns: new[] { "Id", "ConcurrencyStamp", "Name", "NormalizedName" },
                values: new object[,]
                {
                    { new Guid("3f2a6c1e-8b4d-4e7a-9c5f-1a2b3c4d5e01"), "a7c3f1e2-5b8d-4c6a-9e1f-2d4b6a8c0e13", "Client", "CLIENT" },
                    { new Guid("3f2a6c1e-8b4d-4e7a-9c5f-1a2b3c4d5e02"), "b8d4a2f3-6c9e-4d7b-8f2a-3e5c7b9d1f24", "Host", "HOST" },
                    { new Guid("3f2a6c1e-8b4d-4e7a-9c5f-1a2b3c4d5e03"), "c9e5b3a4-7d0f-4e8c-9a3b-4f6d8c0e2a35", "Admin", "ADMIN" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: new Guid("3f2a6c1e-8b4d-4e7a-9c5f-1a2b3c4d5e01"));

            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: new Guid("3f2a6c1e-8b4d-4e7a-9c5f-1a2b3c4d5e02"));

            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: new Guid("3f2a6c1e-8b4d-4e7a-9c5f-1a2b3c4d5e03"));

            migrationBuilder.AddColumn<string>(
                name: "DefaultPersona",
                table: "AspNetUsers",
                type: "text",
                nullable: true);
        }
    }
}
