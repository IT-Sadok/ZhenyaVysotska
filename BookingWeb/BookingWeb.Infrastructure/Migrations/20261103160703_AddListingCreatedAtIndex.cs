using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookingWeb.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddListingCreatedAtIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Listings_CreatedAtUtc_Active",
                table: "Listings",
                columns: new[] { "CreatedAtUtc", "Id" },
                descending: new[] { true, false },
                filter: "\"IsActive\"");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Listings_CreatedAtUtc_Active",
                table: "Listings");
        }
    }
}
