using BookingWeb.Application.Auth;
using BookingWeb.Application.Interfaces;
using BookingWeb.Application.Models;
using BookingWeb.Application.Results;
using BookingWeb.Domain;
using BookingWeb.Domain.Models;
using BookingWeb.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;

namespace BookingWeb.Infrastructure.Identity;

public sealed class IdentityService : IIdentityService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _db;

    public IdentityService(UserManager<ApplicationUser> userManager, ApplicationDbContext db)
    {
        _userManager = userManager;
        _db = db;
    }

    public async Task<Result<UserDto>> RegisterAsync(
        string email, string password, string firstName, string lastName, string role,
        CancellationToken ct = default)
    {
        if (await _userManager.FindByEmailAsync(email) is not null)
            return Result.Failure<UserDto>(AuthErrors.EmailAlreadyUsed);
        
        await using var tx = await _db.Database.BeginTransactionAsync(ct);

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = email,
            UserName = email,
            DefaultPersona = role 
        };

        var created = await _userManager.CreateAsync(user, password);
        if (!created.Succeeded)
        {
            var desc = string.Join("; ", created.Errors.Select(e => e.Description));
            return Result.Failure<UserDto>(new Error("Auth.RegistrationFailed", desc));
        }

        await _userManager.AddToRoleAsync(user, role);
        _db.UserProfiles.Add(UserProfile.Create(user.Id, firstName, lastName));
        await _db.SaveChangesAsync(ct);

        await tx.CommitAsync(ct);

        var roles = await _userManager.GetRolesAsync(user);
        return Result.Success(new UserDto(user.Id, user.Email!, roles.ToList(), user.DefaultPersona));
    }

    public async Task<Result<UserDto>> ValidateCredentialsAsync(
        string email, string password, CancellationToken ct = default)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user is null || !await _userManager.CheckPasswordAsync(user, password))
            return Result.Failure<UserDto>(AuthErrors.InvalidCredentials);

        var roles = await _userManager.GetRolesAsync(user);
        return Result.Success(new UserDto(user.Id, user.Email!, roles.ToList(), user.DefaultPersona));
    }

    public async Task<Result<UserDto>> AddToRoleAsync(Guid userId, string role, CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null)
            return Result.Failure<UserDto>(new Error("User.NotFound", "User is not found."));

        if (!await _userManager.IsInRoleAsync(user, role)) 
        {
            var added = await _userManager.AddToRoleAsync(user, role);
            if (!added.Succeeded)
            {
                var desc = string.Join("; ", added.Errors.Select(e => e.Description));
                return Result.Failure<UserDto>(new Error("Role.AddFailed", desc));
            }
        }

        var roles = await _userManager.GetRolesAsync(user);
        return Result.Success(new UserDto(user.Id, user.Email!, roles.ToList(), user.DefaultPersona));
    }

    public async Task<Result<UserDto>> SetDefaultPersonaAsync(
        Guid userId, string persona, CancellationToken ct = default)
    {
        if (!Personas.IsSwitchable(persona))
            return Result.Failure<UserDto>(
                new Error("Persona.Invalid", $"'{persona}' is not switchable persona."));

        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null)
            return Result.Failure<UserDto>(new Error("User.NotFound", "User is not found."));

        if (!await _userManager.IsInRoleAsync(user, persona))
            return Result.Failure<UserDto>(
                new Error("Persona.NotOwned", $"It not owned '{persona}' — switching is unavailable."));

        user.DefaultPersona = persona;
        await _userManager.UpdateAsync(user);

        var roles = await _userManager.GetRolesAsync(user);
        return Result.Success(new UserDto(user.Id, user.Email!, roles.ToList(), user.DefaultPersona));
    }

    public async Task<Result<UserDto>> GetActiveUserAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null)
            return Result.Failure<UserDto>(new Error("User.NotFound", "User is not found."));

        var roles = await _userManager.GetRolesAsync(user);
        return Result.Success(new UserDto(user.Id, user.Email!, roles.ToList(), user.DefaultPersona));
    }
}