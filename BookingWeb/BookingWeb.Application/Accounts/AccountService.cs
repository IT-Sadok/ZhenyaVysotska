using BookingWeb.Application.Common.Interfaces;
using BookingWeb.Application.Results;
using BookingWeb.Domain;

namespace BookingWeb.Application.Accounts;

public sealed class AccountService : IAccountService
{
    private readonly IIdentityService _identity;
    private readonly IListingRepository _listings;
    private readonly IBookingRepository _bookings;
    private readonly IUserProfileRepository _profiles;
    private readonly IRefreshTokenService _refreshTokens;
    private readonly IDeletedUserRegistry _deletedUsers;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;

    public AccountService(
        IIdentityService identity,
        IListingRepository listings,
        IBookingRepository bookings,
        IUserProfileRepository profiles,
        IRefreshTokenService refreshTokens,
        IDeletedUserRegistry deletedUsers,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _identity = identity;
        _listings = listings;
        _bookings = bookings;
        _profiles = profiles;
        _refreshTokens = refreshTokens;
        _deletedUsers = deletedUsers;
        _unitOfWork = unitOfWork;
        _timeProvider = timeProvider;
    }

    public async Task<Result> DeleteAsync(Guid userId, DeleteAccountRequest request, CancellationToken ct = default)
    {
        var userResult = await _identity.GetUserByIdAsync(userId, ct);
        if (userResult.IsFailure)
        {
            return userResult.Error;
        }
        
        if (userResult.Value.Roles.Contains(Roles.Admin))
        {
            return AccountErrors.AdminCannotBeDeleted;
        }

        if (!await _identity.CheckPasswordAsync(userId, request.Password, ct))
        {
            return AccountErrors.InvalidPassword;
        }

        var now = _timeProvider.GetUtcNow();
        var today = DateOnly.FromDateTime(now.UtcDateTime);

        await using var transaction = await _unitOfWork.BeginTransactionAsync(ct);

        await _listings.DeactivateAllForHostAsync(userId, ct);

        if (await _bookings.HasUpcomingForGuestAsync(userId, today, ct))
        {
            return AccountErrors.HasUpcomingBookingsAsGuest;
        }

        if (await _bookings.HasUpcomingForHostAsync(userId, today, ct))
        {
            return AccountErrors.HasUpcomingBookingsAsHost;
        }

        await _refreshTokens.RevokeAllForUserAsync(userId, ct);

        var profile = await _profiles.GetByUserIdAsync(userId, ct);
        if (profile is not null)
        {
            _profiles.Remove(profile);
        }

        var deleteResult = await _identity.SoftDeleteAsync(userId, now, ct);
        if (deleteResult.IsFailure)
        {
            return deleteResult.Error;
        }

        await _unitOfWork.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        
        _deletedUsers.Add(userId);

        return Result.Success();
    }
}