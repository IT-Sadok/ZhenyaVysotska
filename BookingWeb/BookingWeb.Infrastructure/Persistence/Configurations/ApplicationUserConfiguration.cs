using BookingWeb.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BookingWeb.Infrastructure.Persistence.Configurations;

public sealed class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        builder
            .HasMany(u => u.Roles)
            .WithMany()
            .UsingEntity<IdentityUserRole<Guid>>(
                j => j.HasOne<IdentityRole<Guid>>().WithMany().HasForeignKey(ur => ur.RoleId),
                j => j.HasOne<ApplicationUser>().WithMany().HasForeignKey(ur => ur.UserId)
            );
    }
}