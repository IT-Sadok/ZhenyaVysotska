using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookingWeb.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class BookingsPagingIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Bookings_GuestId_CheckIn",
                table: "Bookings");

            migrationBuilder.DropIndex(
                name: "IX_Bookings_ListingId",
                table: "Bookings");

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_GuestId_CheckIn_Id",
                table: "Bookings",
                columns: new[] { "GuestId", "CheckIn", "Id" },
                descending: new[] { false, true, false });

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_ListingId_CheckIn_Id",
                table: "Bookings",
                columns: new[] { "ListingId", "CheckIn", "Id" },
                descending: new[] { false, true, false });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Bookings_GuestId_CheckIn_Id",
                table: "Bookings");

            migrationBuilder.DropIndex(
                name: "IX_Bookings_ListingId_CheckIn_Id",
                table: "Bookings");

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_GuestId_CheckIn",
                table: "Bookings",
                columns: new[] { "GuestId", "CheckIn" });

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_ListingId",
                table: "Bookings",
                column: "ListingId");
        }
    }
}
