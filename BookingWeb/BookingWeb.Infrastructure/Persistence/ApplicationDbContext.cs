using System.Reflection;
using BookingWeb.Domain.Models;
using BookingWeb.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace BookingWeb.Infrastructure.Persistence;

public class ApplicationDbContext 
    : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
        
    }
    
    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Listing> Listings => Set<Listing>();
    public DbSet<Booking> Bookings => Set<Booking>();
    
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.HasPostgresExtension("btree_gist");
        
        builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }
}