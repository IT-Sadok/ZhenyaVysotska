using BookingWeb.Domain.Models;
using BookingWeb.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BookingWeb.Infrastructure.Persistence.Configurations;

public sealed class BookingConfiguration : IEntityTypeConfiguration<Booking>
{
    public const string NoOverlapConstraintName = "EX_Bookings_NoOverlap";
    
    public void Configure(EntityTypeBuilder<Booking> builder)
    {
        builder.ToTable("Bookings", t =>
        {
            t.HasCheckConstraint("CK_Bookings_CheckOut_AfterCheckIn", "\"CheckOut\" > \"CheckIn\"");
            t.HasCheckConstraint("CK_Bookings_Guests_Positive", "\"Guests\" >= 1");
        });
        
        builder.HasKey(b => b.Id);
        builder.Property(b => b.Id).ValueGeneratedNever();

        builder.Property(b => b.TotalPrice).HasPrecision(18, 2);
        
        builder.Property(b => b.Status)
            .HasConversion<string>()
            .HasMaxLength(32);
        
        builder.Property(b => b.CancelledBy)
            .HasConversion<string>()
            .HasMaxLength(16);

        builder.Ignore(b => b.Nights);

        builder.HasOne<Listing>()
            .WithMany()
            .HasForeignKey(b => b.ListingId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(b => b.GuestId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(b => new { b.GuestId, b.CheckIn });
    }
}