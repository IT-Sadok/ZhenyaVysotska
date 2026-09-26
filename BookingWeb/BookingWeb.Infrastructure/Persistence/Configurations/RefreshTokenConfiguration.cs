using System.ComponentModel;
using BookingWeb.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BookingWeb.Infrastructure.Persistence.Configurations;

public sealed class RefreshTokenConfiguration:IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("RefreshTokens");
        
        builder.HasKey(p => p.Id);
        
        builder.Property(p=>p.TokenHash).HasMaxLength(128).IsRequired();
        builder.HasIndex(t => t.TokenHash).IsUnique(); 
        builder.HasIndex(t => t.UserId);    
    }
}