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
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUserProfileRepository _userProfiles;
    private readonly IUserRepository _users;
 
    public IdentityService(
        UserManager<ApplicationUser> userManager,
        IUnitOfWork unitOfWork,
        IUserProfileRepository userProfiles,
        IUserRepository users)
    {
        _userManager = userManager;
        _unitOfWork = unitOfWork;
        _userProfiles = userProfiles;
        _users = users;
    }
 
    public async Task<Result<UserDto>> RegisterAsync(
        string email, string password, string firstName, string lastName, string role,
        CancellationToken ct = default)
    {
        if (await _userManager.FindByEmailAsync(email) is not null)
        {
            return AuthErrors.EmailAlreadyUsed;
        }
        
        await using var transaction = await _unitOfWork.BeginTransactionAsync(ct);
 
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = email,
            UserName = email
        };
 
        var createResult = await _userManager.CreateAsync(user, password);
        if (!createResult.Succeeded)
        {
            return UserErrors.RegistrationFailed(Describe(createResult));
        }
 
        var addRoleResult = await _userManager.AddToRoleAsync(user, role);
        if (!addRoleResult.Succeeded)
        {
            return UserErrors.RoleAssignmentFailed(Describe(addRoleResult));
        }
        
        _userProfiles.Add(UserProfile.Create(user.Id, firstName, lastName));
        await _unitOfWork.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
 
        return await ToUserDtoAsync(user);
    }
 
    public async Task<Result<UserDto>> ValidateCredentialsAsync(
        string email, string password, CancellationToken ct = default)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user is null || !await _userManager.CheckPasswordAsync(user, password))
        {
            return AuthErrors.InvalidCredentials;
        }
 
        return await ToUserDtoAsync(user);
    }
 
    public async Task<Result<UserDto>> AddToRoleAsync(Guid userId, string role, CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return UserErrors.NotFound;
        }
 
        if (!await _userManager.IsInRoleAsync(user, role))
        {
            var addRoleResult = await _userManager.AddToRoleAsync(user, role);
            if (!addRoleResult.Succeeded)
            {
                return UserErrors.RoleAssignmentFailed(Describe(addRoleResult));
            }
        }
 
        return await ToUserDtoAsync(user);
    }
 
    public async Task<Result<UserDto>> GetUserByIdAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null)
        {
            return UserErrors.NotFound;
        }
 
        return await ToUserDtoAsync(user);
    }
 
    public Task<PagedResult<UserDto>> GetUsersAsync(UserFilterDto filter, CancellationToken ct = default)
    {
        return _users.GetUsersAsync(filter, ct);
    }

    private async Task<UserDto> ToUserDtoAsync(ApplicationUser user)
    {
        var roles = await _userManager.GetRolesAsync(user);
        return new UserDto(user.Id, user.Email!, roles.ToList());
    }
 
    private static string Describe(IdentityResult result)
    {
        return string.Join("; ", result.Errors.Select(error => error.Description));
    }
}