using BookingWeb.Application.Common.Interfaces;
using BookingWeb.Application.Profiles.Requests;
using BookingWeb.Application.Results;
using Mapster;

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
        return profile.Adapt<ProfileDto>();
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

        return profile.Adapt<ProfileDto>();
    }
    
}