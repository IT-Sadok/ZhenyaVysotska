using BookingWeb.Domain;
using BookingWeb.Domain.Models;
using BookingWeb.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BookingWeb.Infrastructure.Persistence;

public static class DbSeeder
{
    public static async Task SeedAsync(IServiceProvider services, CancellationToken ct = default)
    {
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var db = services.GetRequiredService<ApplicationDbContext>();
        var configuration = services.GetRequiredService<IConfiguration>();

        var adminEmail = configuration["Seed:AdminEmail"] ?? "admin@bookingweb.local";
        var adminPassword = configuration["Seed:AdminPassword"] ?? "Admin123!";

        if (await userManager.FindByIdAsync(SeedIds.AdminUserId.ToString()) is not null)
        {
            return;
        }

        var admin = new ApplicationUser
        {
            Id = SeedIds.AdminUserId,
            Email = adminEmail,
            UserName = adminEmail,
            EmailConfirmed = true
        };

        var createResult = await userManager.CreateAsync(admin, adminPassword);
        EnsureSucceeded(createResult, "create admin user");

        var addRoleResult = await userManager.AddToRoleAsync(admin, Roles.Admin);
        EnsureSucceeded(addRoleResult, "assign Admin role");

        db.UserProfiles.Add(UserProfile.Create(admin.Id, "System", "Administrator"));
        await db.SaveChangesAsync(ct);
    }

    private static void EnsureSucceeded(IdentityResult result, string action)
    {
        if (result.Succeeded)
        {
            return;
        }

        var errors = string.Join("; ", result.Errors.Select(error => error.Description));
        throw new InvalidOperationException($"Seeding failed to {action}: {errors}");
    }
}