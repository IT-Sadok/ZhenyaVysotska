using BookingWeb.Application.Models;
using BookingWeb.Application.Profiles.Requests;
using BookingWeb.Application.Results;

namespace BookingWeb.Application.Profiles;

public interface IProfileService
{
    Task<Result<ProfileDto>> GetAsync(Guid userId, CancellationToken ct = default);

    Task<Result<ProfileDto>> UpdateAsync(
        Guid userId, UpdateProfileRequest request, CancellationToken ct = default);
}