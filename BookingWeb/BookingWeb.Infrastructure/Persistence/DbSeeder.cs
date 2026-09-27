using BookingWeb.Domain;
using BookingWeb.Domain.Models;
using BookingWeb.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BookingWeb.Infrastructure.Persistence;

public static class DbSeeder
{
    public static async Task SeedAsync(IServiceProvider sp, CancellationToken ct = default)
    {
        var roleManager = sp.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        var userManager = sp.GetRequiredService<UserManager<ApplicationUser>>();
        var db = sp.GetRequiredService<ApplicationDbContext>();
        var config = sp.GetRequiredService<IConfiguration>();

        foreach (var role in Roles.All)
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole<Guid>(role));

        var adminEmail = config["Seed:AdminEmail"] ?? "admin@bookingweb.local";
        var adminPassword = config["Seed:AdminPassword"] ?? "Admin123!";

        if (await userManager.FindByEmailAsync(adminEmail) is null)
        {
            var admin = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                Email = adminEmail,
                UserName = adminEmail,
                EmailConfirmed = true,
                DefaultPersona = null
            };

            var created = await userManager.CreateAsync(admin, adminPassword);
            if (created.Succeeded)
            {
                await userManager.AddToRoleAsync(admin, Roles.Admin);
                db.UserProfiles.Add(UserProfile.Create(admin.Id, "System", "Administrator"));
                await db.SaveChangesAsync(ct);
            }
        }
    }
}