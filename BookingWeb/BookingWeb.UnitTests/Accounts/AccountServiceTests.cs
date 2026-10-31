using BookingWeb.Application.Accounts;
using BookingWeb.Application.Auth;
using BookingWeb.Application.Common.Interfaces;
using BookingWeb.Application.Results;
using BookingWeb.Domain;
using BookingWeb.Domain.Models;
using BookingWeb.UnitTests.TestDoubles;
using Moq;
using Shouldly;

namespace BookingWeb.UnitTests.Accounts;

public sealed class AccountServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 3, 1, 9, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly Today = new(2026, 3, 1);
    private static readonly DeleteAccountRequest Request = new("Password1!");

    private readonly Guid _userId = Guid.NewGuid();
    private readonly Mock<IIdentityService> _identity = new();
    private readonly Mock<IListingRepository> _listings = new();
    private readonly Mock<IBookingRepository> _bookings = new();
    private readonly Mock<IUserProfileRepository> _profiles = new();
    private readonly Mock<IRefreshTokenService> _refreshTokens = new();
    private readonly Mock<IDeletedUserRegistry> _deletedUsers = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IUnitOfWorkTransaction> _transaction = new();
    private readonly AccountService _sut;

    public AccountServiceTests()
    {
        SetupUser(Roles.Client);
        _identity
            .Setup(identity => identity.CheckPasswordAsync(_userId, Request.Password, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _identity
            .Setup(identity => identity.SoftDeleteAsync(_userId, Now, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());
        _unitOfWork
            .Setup(unitOfWork => unitOfWork.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(_transaction.Object);

        _sut = new AccountService(
            _identity.Object,
            _listings.Object,
            _bookings.Object,
            _profiles.Object,
            _refreshTokens.Object,
            _deletedUsers.Object,
            _unitOfWork.Object,
            new FixedTimeProvider(Now));
    }

    private void SetupUser(params string[] roles)
    {
        _identity
            .Setup(identity => identity.GetUserByIdAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(new UserDto(_userId, "user@test.com", roles)));
    }

    private void VerifyNothingWasDeleted()
    {
        _identity.Verify(
            identity => identity.SoftDeleteAsync(It.IsAny<Guid>(), It.IsAny<DateTimeOffset>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _transaction.Verify(transaction => transaction.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
        _deletedUsers.Verify(registry => registry.Add(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task Delete_ShouldDeleteCommitAndMarkUser_WhenPasswordIsCorrectAndThereAreNoBookings()
    {
        var profile = UserProfile.Create(_userId, "Ivan", "Petrenko");
        _profiles
            .Setup(repository => repository.GetByUserIdAsync(_userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);

        var result = await _sut.DeleteAsync(_userId, Request);

        result.IsSuccess.ShouldBeTrue();
        _listings.Verify(repository => repository.DeactivateAllForHostAsync(_userId, It.IsAny<CancellationToken>()), Times.Once);
        _refreshTokens.Verify(service => service.RevokeAllForUserAsync(_userId, It.IsAny<CancellationToken>()), Times.Once);
        _profiles.Verify(repository => repository.Remove(profile), Times.Once);
        _transaction.Verify(transaction => transaction.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
        _deletedUsers.Verify(registry => registry.Add(_userId), Times.Once);
    }

    [Fact]
    public async Task Delete_ShouldReturnForbidden_WhenUserIsAdmin()
    {
        SetupUser(Roles.Admin);

        var result = await _sut.DeleteAsync(_userId, Request);

        result.Error.ShouldBe(AccountErrors.AdminCannotBeDeleted);
        result.Error.Type.ShouldBe(ErrorType.Forbidden);
        VerifyNothingWasDeleted();
    }

    [Fact]
    public async Task Delete_ShouldReturnInvalidPasswordAndNotStartTransaction_WhenPasswordIsWrong()
    {
        _identity
            .Setup(identity => identity.CheckPasswordAsync(_userId, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await _sut.DeleteAsync(_userId, Request);

        result.Error.ShouldBe(AccountErrors.InvalidPassword);
        _unitOfWork.Verify(unitOfWork => unitOfWork.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
        VerifyNothingWasDeleted();
    }

    [Fact]
    public async Task Delete_ShouldReturnConflictWithoutCommit_WhenGuestHasUpcomingBookings()
    {
        _bookings
            .Setup(repository => repository.HasUpcomingForGuestAsync(_userId, Today, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _sut.DeleteAsync(_userId, Request);

        result.Error.ShouldBe(AccountErrors.HasUpcomingBookingsAsGuest);
        VerifyNothingWasDeleted();
        _transaction.Verify(transaction => transaction.DisposeAsync(), Times.Once);
    }

    [Fact]
    public async Task Delete_ShouldReturnConflictWithoutCommit_WhenHostListingsHaveUpcomingBookings()
    {
        SetupUser(Roles.Host);
        _bookings
            .Setup(repository => repository.HasUpcomingForHostAsync(_userId, Today, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _sut.DeleteAsync(_userId, Request);

        result.Error.ShouldBe(AccountErrors.HasUpcomingBookingsAsHost);
        VerifyNothingWasDeleted();
    }

    [Fact]
    public async Task Delete_ShouldNotMarkUser_WhenSoftDeleteFails()
    {
        _identity
            .Setup(identity => identity.SoftDeleteAsync(_userId, Now, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure(AccountErrors.DeletionFailed("boom")));

        var result = await _sut.DeleteAsync(_userId, Request);

        result.IsFailure.ShouldBeTrue();
        _transaction.Verify(transaction => transaction.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
        _deletedUsers.Verify(registry => registry.Add(It.IsAny<Guid>()), Times.Never);
    }
}
