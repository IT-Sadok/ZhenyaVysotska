using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BookingWeb.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddListingTimeZone : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TimeZoneId",
                table: "Listings",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TimeZoneId",
                table: "Listings");
        }
    }
}
