using BookingWeb.Domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BookingWeb.Infrastructure.Persistence.Configurations;

public sealed class UserProfileConfiguration : IEntityTypeConfiguration<UserProfile>
{
    public void Configure(EntityTypeBuilder<UserProfile> builder)
    {
        builder.ToTable("UserProfiles");
        
        builder.HasKey(p => p.UserId);
        builder.Property(p => p.UserId).ValueGeneratedNever();
        
        builder.Property(p => p.FirstName).HasMaxLength(128).IsRequired();
        builder.Property(p => p.LastName).HasMaxLength(128).IsRequired();
        builder.Property(p => p.Bio).HasMaxLength(1000);
        builder.Property(p => p.AvatarUrl).HasMaxLength(500);
    }
}