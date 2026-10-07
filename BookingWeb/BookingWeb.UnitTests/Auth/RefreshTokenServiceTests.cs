using BookingWeb.Application.Auth;
using BookingWeb.Application.Common.Settings;
using BookingWeb.Application.Interfaces;
using BookingWeb.Domain.Models;
using BookingWeb.UnitTests.TestDoubles;
using Microsoft.Extensions.Options;
using Moq;
using Shouldly;

namespace BookingWeb.UnitTests.Auth;

public sealed class RefreshTokenServiceTests
{
    private const int ExpiryDays = 7;
    private static readonly DateTimeOffset Now = new(2026, 1, 15, 12, 0, 0, TimeSpan.Zero);

    private readonly Mock<IRefreshTokenRepository> _repository = new();
    private readonly FixedTimeProvider _timeProvider = new(Now);
    private readonly RefreshTokenService _sut;

    public RefreshTokenServiceTests()
    {
        var settings = Options.Create(new RefreshTokenSettings { ExpiryDays = ExpiryDays });
        _sut = new RefreshTokenService(_repository.Object, settings, _timeProvider);
    }

    private RefreshToken ArrangeStoredToken(Guid userId)
    {
        var storedToken = RefreshToken.Create(userId, "stored-hash", Now, Now.AddDays(ExpiryDays));

        _repository
            .Setup(repository => repository.GetByHashAsync(
                It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(storedToken);

        return storedToken;
    }

    [Fact]
    public async Task Issue_ShouldStoreHashInsteadOfRawToken()
    {
        RefreshToken? addedToken = null;
        _repository
            .Setup(repository => repository.Add(It.IsAny<RefreshToken>()))
            .Callback<RefreshToken>(token => addedToken = token);

        var issued = await _sut.IssueAsync(Guid.NewGuid());

        addedToken.ShouldNotBeNull();
        addedToken.TokenHash.ShouldNotBe(issued.Token);
    }

    [Fact]
    public async Task Issue_ShouldSetExpiryFromSettings()
    {
        RefreshToken? addedToken = null;
        _repository
            .Setup(repository => repository.Add(It.IsAny<RefreshToken>()))
            .Callback<RefreshToken>(token => addedToken = token);

        var issued = await _sut.IssueAsync(Guid.NewGuid());

        issued.ExpiresAtUtc.ShouldBe(Now.AddDays(ExpiryDays));
        addedToken!.ExpiresAtUtc.ShouldBe(Now.AddDays(ExpiryDays));
    }

    [Fact]
    public async Task Issue_ShouldReturnDifferentTokens_WhenCalledTwice()
    {
        var first = await _sut.IssueAsync(Guid.NewGuid());
        var second = await _sut.IssueAsync(Guid.NewGuid());

        first.Token.ShouldNotBe(second.Token);
    }

    [Fact]
    public async Task Validate_ShouldReturnUserId_WhenIssuedTokenIsPresented()
    {
        var userId = Guid.NewGuid();
        RefreshToken? addedToken = null;
        _repository
            .Setup(repository => repository.Add(It.IsAny<RefreshToken>()))
            .Callback<RefreshToken>(token => addedToken = token);

        var issued = await _sut.IssueAsync(userId);

        _repository
            .Setup(repository => repository.GetByHashAsync(
                addedToken!.TokenHash, It.IsAny<CancellationToken>()))
            .ReturnsAsync(addedToken);

        var result = await _sut.ValidateAsync(issued.Token);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(userId);
    }

    [Fact]
    public async Task Validate_ShouldReturnInvalid_WhenTokenIsUnknown()
    {
        _repository
            .Setup(repository => repository.GetByHashAsync(
                It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((RefreshToken?)null);

        var result = await _sut.ValidateAsync("unknown-token");

        result.Error.ShouldBe(RefreshTokenErrors.Invalid);
    }

    [Fact]
    public async Task Validate_ShouldRevokeAllSessionsAndReturnReused_WhenTokenWasAlreadyRevoked()
    {
        var userId = Guid.NewGuid();
        var storedToken = ArrangeStoredToken(userId);
        storedToken.Revoke(Now);

        var result = await _sut.ValidateAsync("reused-token");

        result.Error.ShouldBe(RefreshTokenErrors.Reused);
        _repository.Verify(
            repository => repository.RevokeAllForUserAsync(userId, Now, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Validate_ShouldReturnExpired_WhenExpiryHasPassed()
    {
        ArrangeStoredToken(Guid.NewGuid());
        _timeProvider.Advance(TimeSpan.FromDays(ExpiryDays + 1));

        var result = await _sut.ValidateAsync("old-token");

        result.Error.ShouldBe(RefreshTokenErrors.Expired);
    }

    [Fact]
    public async Task Validate_ShouldReturnExpired_WhenNowEqualsExpiry()
    {
        ArrangeStoredToken(Guid.NewGuid());
        _timeProvider.Advance(TimeSpan.FromDays(ExpiryDays));

        var result = await _sut.ValidateAsync("token");

        result.Error.ShouldBe(RefreshTokenErrors.Expired);
    }

    [Fact]
    public async Task Validate_ShouldNotRevokeSessions_WhenTokenIsExpired()
    {
        ArrangeStoredToken(Guid.NewGuid());
        _timeProvider.Advance(TimeSpan.FromDays(ExpiryDays + 1));

        await _sut.ValidateAsync("old-token");

        _repository.Verify(
            repository => repository.RevokeAllForUserAsync(It.IsAny<Guid>(), It.IsAny<DateTimeOffset>(), 
                It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Revoke_ShouldMarkTokenRevokedAtCurrentTime_WhenTokenExists()
    {
        var storedToken = ArrangeStoredToken(Guid.NewGuid());

        await _sut.RevokeAsync("token");

        storedToken.IsRevoked.ShouldBeTrue();
        storedToken.RevokedAtUtc.ShouldBe(Now);
    }

    [Fact]
    public async Task Revoke_ShouldDoNothing_WhenTokenIsUnknown()
    {
        _repository
            .Setup(repository => repository.GetByHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((RefreshToken?)null);

        await _sut.RevokeAsync("unknown-token");
    }

    [Fact]
    public async Task RevokeAllForUser_ShouldPassCurrentTimeToRepository()
    {
        var userId = Guid.NewGuid();

        await _sut.RevokeAllForUserAsync(userId);

        _repository.Verify(
            repository => repository.RevokeAllForUserAsync(userId, Now, It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
