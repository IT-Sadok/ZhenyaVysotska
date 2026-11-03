using BookingWeb.Domain.Entities;
using BookingWeb.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NetTopologySuite.Geometries;

namespace BookingWeb.Infrastructure.Persistence.Configurations;

public sealed class ListingConfiguration : IEntityTypeConfiguration<Listing>
{
    public const string PublicGeom = "PublicGeom";
    
    public const int Wgs84Srid = 4326;

    public const string CreatedAtActiveIndex = "IX_Listings_CreatedAtUtc_Active";

    public void Configure(EntityTypeBuilder<Listing> builder)
    {
        builder.ToTable("Listings", t =>
        {
            t.HasCheckConstraint("CK_Listings_PricePerNight_Positive", "\"PricePerNight\" > 0");
            t.HasCheckConstraint("CK_Listings_MaxGuests_Positive", "\"MaxGuests\" >= 1");

            t.HasCheckConstraint("CK_Listings_Latitude_Range", "\"Latitude\" BETWEEN -90 AND 90");
            t.HasCheckConstraint("CK_Listings_Longitude_Range", "\"Longitude\" BETWEEN -180 AND 180");
            t.HasCheckConstraint("CK_Listings_PublicLatitude_Range", "\"PublicLatitude\" BETWEEN -90 AND 90");
            t.HasCheckConstraint("CK_Listings_PublicLongitude_Range", "\"PublicLongitude\" BETWEEN -180 AND 180");
            t.HasCheckConstraint("CK_Listings_CountryCode_Format", "\"CountryCode\" ~ '^[A-Z]{2}$'");
        });

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(l => l.Title).HasMaxLength(200).IsRequired();
        builder.Property(l => l.Description).HasMaxLength(2000);
        builder.Property(l => l.CountryCode).HasMaxLength(Listing.CountryCodeLength).IsFixedLength().IsRequired();
        builder.Property(l => l.Region).HasMaxLength(100);
        builder.Property(l => l.City).HasMaxLength(100).IsRequired();
        builder.Property(l => l.Address).HasMaxLength(300).IsRequired();
        builder.Property(l => l.PricePerNight).HasPrecision(18, 2);

        builder.ComplexProperty(l => l.Location, location =>
        {
            location.Property(p => p.Latitude).HasColumnName("Latitude");
            location.Property(p => p.Longitude).HasColumnName("Longitude");
        });
        builder.ComplexProperty(l => l.PublicLocation, location =>
        {
            location.Property(p => p.Latitude).HasColumnName("PublicLatitude");
            location.Property(p => p.Longitude).HasColumnName("PublicLongitude");
        });

        builder.Property<Point>(PublicGeom)
            .HasColumnType($"geometry(Point, {Wgs84Srid})")
            .HasComputedColumnSql(
                $"ST_SetSRID(ST_MakePoint(\"PublicLongitude\", \"PublicLatitude\"), {Wgs84Srid})",
                stored: true);

        builder.HasIndex(PublicGeom)
            .HasMethod("gist")
            .HasFilter("\"IsActive\"")
            .HasDatabaseName("IX_Listings_PublicGeom_Active");

        builder.HasIndex(l => new { l.CreatedAtUtc, l.Id })
            .IsDescending(true, false)
            .HasFilter("\"IsActive\"")
            .HasDatabaseName(CreatedAtActiveIndex);

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(l => l.HostId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
