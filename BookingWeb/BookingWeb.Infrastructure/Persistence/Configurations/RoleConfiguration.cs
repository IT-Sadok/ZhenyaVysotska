using BookingWeb.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BookingWeb.Infrastructure.Persistence.Configurations;

public sealed class RoleConfiguration : IEntityTypeConfiguration<IdentityRole<Guid>>
{
    public void Configure(EntityTypeBuilder<IdentityRole<Guid>> builder)
    {
        builder.HasData(
            CreateRole(SeedIds.ClientRoleId, Roles.Client, "a7c3f1e2-5b8d-4c6a-9e1f-2d4b6a8c0e13"),
            CreateRole(SeedIds.HostRoleId, Roles.Host, "b8d4a2f3-6c9e-4d7b-8f2a-3e5c7b9d1f24"),
            CreateRole(SeedIds.AdminRoleId, Roles.Admin, "c9e5b3a4-7d0f-4e8c-9a3b-4f6d8c0e2a35"));
    }

    private static IdentityRole<Guid> CreateRole(Guid id, string name, string concurrencyStamp)
    {
        return new IdentityRole<Guid>
        {
            Id = id,
            Name = name,
            NormalizedName = name.ToUpperInvariant(),
            ConcurrencyStamp = concurrencyStamp
        };
    }
}