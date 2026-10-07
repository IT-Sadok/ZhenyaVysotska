using BookingWeb.Application.Interfaces;
using BookingWeb.Application.Models;
using BookingWeb.Application.Profiles.Requests;
using BookingWeb.Application.Results;
using FluentValidation;

namespace BookingWeb.Application.Profiles;

public class ProfileService : IProfileService
{
    private readonly IUserProfileRepository _profiles;
    private readonly IUnitOfWork _unitOfWork;

    public ProfileService(IUserProfileRepository profiles, IUnitOfWork unitOfWork)
    {
        _profiles = profiles;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<ProfileDto>> GetAsync(Guid userId, CancellationToken ct = default)
    {
        var profile = await _profiles.GetByUserIdAsync(userId, ct);

        if (profile is null)
        {
            return new Error("Profile.NotFound", "Profile not found", ErrorType.NotFound);
        }
        return new ProfileDto(profile.UserId, profile.FirstName, profile.LastName, 
            profile.Bio, profile.AvatarUrl);
    }
    
    public async Task<Result<ProfileDto>> UpdateAsync(
        Guid userId, UpdateProfileRequest request, CancellationToken ct = default)
    {
        var profile = await _profiles.GetByUserIdAsync(userId, ct);
        if (profile is null)
            return new Error("Profile.NotFound", "Profile not found", ErrorType.NotFound);

        profile.UpdateName(request.FirstName, request.LastName); 
        profile.UpdateBio(request.Bio);
        profile.UpdateAvatar(request.AvatarUrl);
        
        await _unitOfWork.SaveChangesAsync(ct);

        return new ProfileDto(profile.UserId, profile.FirstName, 
            profile.LastName, profile.Bio, profile.AvatarUrl);
    }
    
}