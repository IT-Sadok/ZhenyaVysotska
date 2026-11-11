using Microsoft.EntityFrameworkCore.Migrations;
using NetTopologySuite.Geometries;

#nullable disable

namespace BookingWeb.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddListingLocation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""DROP INDEX "IX_Listings_City_Lower";""");

            // Learning project: there is no production data. Existing listings have no
            // coordinates and there is no source to backfill them from, so the table is
            // cleared explicitly before adding NOT NULL columns.
            migrationBuilder.Sql("""DELETE FROM "Listings";""");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:postgis", ",,");

            migrationBuilder.AddColumn<double>(
                name: "Latitude",
                table: "Listings",
                type: "double precision",
                nullable: false);

            migrationBuilder.AddColumn<double>(
                name: "Longitude",
                table: "Listings",
                type: "double precision",
                nullable: false);

            migrationBuilder.AddColumn<double>(
                name: "PublicLatitude",
                table: "Listings",
                type: "double precision",
                nullable: false);

            migrationBuilder.AddColumn<double>(
                name: "PublicLongitude",
                table: "Listings",
                type: "double precision",
                nullable: false);

            migrationBuilder.AddColumn<string>(
                name: "Region",
                table: "Listings",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<Point>(
                name: "PublicGeom",
                table: "Listings",
                type: "geometry(Point, 4326)",
                nullable: true,
                computedColumnSql: "ST_SetSRID(ST_MakePoint(\"PublicLongitude\", \"PublicLatitude\"), 4326)",
                stored: true);

            migrationBuilder.CreateIndex(
                name: "IX_Listings_CreatedAtUtc_Active",
                table: "Listings",
                columns: new[] { "CreatedAtUtc", "Id" },
                descending: new[] { true, false },
                filter: "\"IsActive\"");

            migrationBuilder.CreateIndex(
                name: "IX_Listings_PublicGeom_Active",
                table: "Listings",
                column: "PublicGeom",
                filter: "\"IsActive\"")
                .Annotation("Npgsql:IndexMethod", "gist");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Listings_Latitude_Range",
                table: "Listings",
                sql: "\"Latitude\" BETWEEN -90 AND 90");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Listings_Longitude_Range",
                table: "Listings",
                sql: "\"Longitude\" BETWEEN -180 AND 180");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Listings_PublicLatitude_Range",
                table: "Listings",
                sql: "\"PublicLatitude\" BETWEEN -90 AND 90");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Listings_PublicLongitude_Range",
                table: "Listings",
                sql: "\"PublicLongitude\" BETWEEN -180 AND 180");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""CREATE INDEX "IX_Listings_City_Lower" ON "Listings" (lower("City"));""");

            migrationBuilder.DropIndex(
                name: "IX_Listings_CreatedAtUtc_Active",
                table: "Listings");

            migrationBuilder.DropIndex(
                name: "IX_Listings_PublicGeom_Active",
                table: "Listings");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Listings_Latitude_Range",
                table: "Listings");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Listings_Longitude_Range",
                table: "Listings");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Listings_PublicLatitude_Range",
                table: "Listings");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Listings_PublicLongitude_Range",
                table: "Listings");

            migrationBuilder.DropColumn(
                name: "PublicGeom",
                table: "Listings");

            migrationBuilder.DropColumn(
                name: "Latitude",
                table: "Listings");

            migrationBuilder.DropColumn(
                name: "Longitude",
                table: "Listings");

            migrationBuilder.DropColumn(
                name: "PublicLatitude",
                table: "Listings");

            migrationBuilder.DropColumn(
                name: "PublicLongitude",
                table: "Listings");

            migrationBuilder.DropColumn(
                name: "Region",
                table: "Listings");

            migrationBuilder.AlterDatabase()
                .OldAnnotation("Npgsql:PostgresExtension:postgis", ",,");
        }
    }
}
