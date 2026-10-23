using BookingWeb.Domain.Models;
using BookingWeb.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BookingWeb.Infrastructure.Persistence.Configurations;

public sealed class ListingConfiguration : IEntityTypeConfiguration<Listing>
{
    public void Configure(EntityTypeBuilder<Listing> builder)
    {
        builder.ToTable("Listings", t =>
        {
            t.HasCheckConstraint("CK_Listings_PricePerNight_Positive", "\"PricePerNight\" > 0");
            t.HasCheckConstraint("CK_Listings_MaxGuests_Positive", "\"MaxGuests\" >= 1");
        });
        
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        
        builder.Property(l => l.Title).HasMaxLength(200).IsRequired();
        builder.Property(l => l.Description).HasMaxLength(2000);
        builder.Property(l => l.City).HasMaxLength(100).IsRequired();
        builder.Property(l => l.Address).HasMaxLength(300).IsRequired();
        builder.Property(l => l.PricePerNight).HasPrecision(18, 2);
        
        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(l => l.HostId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(l => l.HostId);
    }
}