using System.Net;
using System.Net.Http.Json;
using BookingWeb.Application.Listings.Requests;
using BookingWeb.Domain;
using BookingWeb.Infrastructure.Persistence;
using BookingWeb.Infrastructure.Persistence.Configurations;
using BookingWeb.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NetTopologySuite.Geometries;
using Npgsql;
using Shouldly;

namespace BookingWeb.IntegrationTests.Persistence;

public sealed class ListingPersistenceTests : ApiTestBase
{
    public ListingPersistenceTests(ApiFactory factory) : base(factory)
    {
    }

    private sealed record StoredLocation(
        double Latitude, double Longitude, double PublicLatitude, double PublicLongitude, Point PublicGeom);

    private async Task<StoredLocation> ReadStoredLocationAsync(Guid listingId)
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        return await db.Listings
            .AsNoTracking()
            .Where(l => l.Id == listingId)
            .Select(l => new StoredLocation(
                l.Location.Latitude,
                l.Location.Longitude,
                l.PublicLocation.Latitude,
                l.PublicLocation.Longitude,
                EF.Property<Point>(l, ListingConfiguration.PublicGeom)))
            .SingleAsync();
    }

    [Fact]
    public async Task CreateListing_ShouldStoreExactLocationAndComputePublicGeomFromPublicPoint_WhenRequestIsValid()
    {
        using var host = await CreateUserClientAsync("host@test.com", Roles.Host);

        var listing = await CreateListingAsync(host);

        var stored = await ReadStoredLocationAsync(listing.Id);
        stored.Latitude.ShouldBe(KyivLatitude);
        stored.Longitude.ShouldBe(KyivLongitude);
        (stored.PublicLatitude, stored.PublicLongitude).ShouldNotBe((KyivLatitude, KyivLongitude));

        stored.PublicGeom.SRID.ShouldBe(ListingConfiguration.Wgs84Srid);
        stored.PublicGeom.X.ShouldBe(stored.PublicLongitude);
        stored.PublicGeom.Y.ShouldBe(stored.PublicLatitude);
    }

    [Fact]
    public async Task UpdateListing_ShouldRecomputePublicGeom_WhenCoordinatesChanged()
    {
        using var host = await CreateUserClientAsync("host@test.com", Roles.Host);
        var listing = await CreateListingAsync(host);
        var before = await ReadStoredLocationAsync(listing.Id);
        var update = new ListingRequest(
            "Sea view", null, "Odesa Oblast", "Odesa", "Deribasivska 1", 46.4825, 30.7233, 2500m, 6);

        var response = await host.PutAsJsonAsync($"/api/listings/{listing.Id}", update);

        await response.ShouldHaveStatusAsync(HttpStatusCode.OK);
        var after = await ReadStoredLocationAsync(listing.Id);
        after.Latitude.ShouldBe(46.4825);
        after.PublicGeom.ShouldNotBe(before.PublicGeom);
        after.PublicGeom.X.ShouldBe(after.PublicLongitude);
        after.PublicGeom.Y.ShouldBe(after.PublicLatitude);
    }

    [Fact]
    public async Task CreatedAtIndex_ShouldMatchSearchOrderAndCoverOnlyActiveListings()
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var definition = await db.Database
            .SqlQuery<string>($"""
                SELECT indexdef AS "Value" FROM pg_indexes
                WHERE tablename = 'Listings' AND indexname = {ListingConfiguration.CreatedAtActiveIndex}
                """)
            .SingleAsync();

        definition.ShouldContain("""USING btree ("CreatedAtUtc" DESC, "Id")""");
        definition.ShouldEndWith("WHERE \"IsActive\"");
    }

    [Theory]
    [InlineData("""UPDATE "Listings" SET "Latitude" = 91""", "CK_Listings_Latitude_Range")]
    [InlineData("""UPDATE "Listings" SET "PublicLongitude" = -181""", "CK_Listings_PublicLongitude_Range")]
    public async Task RawUpdate_ShouldBeRejectedByCheckConstraint_WhenValueBypassesDomain(
        string sql, string constraint)
    {
        using var host = await CreateUserClientAsync("host@test.com", Roles.Host);
        await CreateListingAsync(host);
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var exception = await Should.ThrowAsync<PostgresException>(() => db.Database.ExecuteSqlRawAsync(sql));

        exception.SqlState.ShouldBe(PostgresErrorCodes.CheckViolation);
        exception.ConstraintName.ShouldBe(constraint);
    }
}
