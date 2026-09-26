using BookingWeb.Application.Interfaces;
using BookingWeb.Application.Models;
using BookingWeb.Application.Profiles.Requests;
using BookingWeb.Application.Results;
using FluentValidation;

namespace BookingWeb.Application.Profiles;

public class ProfileService
{
    private readonly IEnumerable<IValidator<UpdateProfileRequest>> _validators;
    private readonly IUserProfileRepository _profiles;
    private readonly IUnitOfWork _unitOfWork;

    public ProfileService(
        IEnumerable<IValidator<UpdateProfileRequest>> validators, 
        IUserProfileRepository profiles,
        IUnitOfWork unitOfWork)
    {
        _validators = validators;
        _profiles = profiles;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<ProfileDto>> GetAsync(Guid userId, CancellationToken ct = default)
    {
        var profile = await _profiles.GetByUserIdAsync(userId, ct);

        return profile is null
            ? Result.Failure<ProfileDto>(new Error("Profile.NotFound", "Profile not found"))
            : Result.Success(new ProfileDto(
                profile.UserId, profile.FirstName, profile.LastName, profile.Bio, profile.AvatarUrl));
    }
    
    public async Task<Result<ProfileDto>> UpdateAsync(
        Guid userId, UpdateProfileRequest request, CancellationToken ct = default)
    {
        var validationError = await _validators.ValidateAllAsync(request, ct);
        if (validationError is not null)
            return Result.Failure<ProfileDto>(validationError);

        var profile = await _profiles.GetByUserIdAsync(userId, ct);
        if (profile is null)
            return Result.Failure<ProfileDto>(new Error("Profile.NotFound", "Profile not found"));

        profile.UpdateName(request.FirstName, request.LastName); 
        profile.UpdateBio(request.Bio);
        profile.UpdateAvatar(request.AvatarUrl);
        
        await _unitOfWork.SaveChangesAsync(ct);

        return Result.Success(new ProfileDto(
            profile.UserId, profile.FirstName, profile.LastName, profile.Bio, profile.AvatarUrl));
    }
    
}