using BookingWeb.Domain.Models;
using Shouldly;

namespace BookingWeb.UnitTests.Domain;

public sealed class RefreshTokenTests
{
    private static readonly DateTimeOffset CreatedAt = new(2026, 1, 15, 12, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset ExpiresAt = CreatedAt.AddDays(7);

    private static RefreshToken CreateToken()
    {
        return RefreshToken.Create(Guid.NewGuid(), "hash", CreatedAt, ExpiresAt);
    }

    [Fact]
    public void Create_ShouldCreateActiveToken_WhenArgumentsAreValid()
    {
        var userId = Guid.NewGuid();

        var token = RefreshToken.Create(userId, "hash", CreatedAt, ExpiresAt);

        token.Id.ShouldNotBe(Guid.Empty);
        token.UserId.ShouldBe(userId);
        token.TokenHash.ShouldBe("hash");
        token.IsRevoked.ShouldBeFalse();
    }

    [Fact]
    public void Create_ShouldThrow_WhenUserIdIsEmpty()
    {
        Should.Throw<ArgumentException>(() => RefreshToken.Create(
            Guid.Empty, "hash", CreatedAt, ExpiresAt));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_ShouldThrow_WhenTokenHashIsBlank(string tokenHash)
    {
        Should.Throw<ArgumentException>(() => RefreshToken.Create(
            Guid.NewGuid(), tokenHash, CreatedAt, ExpiresAt));
    }

    [Fact]
    public void Create_ShouldThrow_WhenExpiryIsNotAfterCreation()
    {
        Should.Throw<ArgumentException>(() => RefreshToken.Create(
            Guid.NewGuid(), "hash", CreatedAt, CreatedAt));
    }

    [Fact]
    public void Revoke_ShouldSetRevocationTime_WhenTokenIsActive()
    {
        var token = CreateToken();
        var revokedAt = CreatedAt.AddHours(1);

        token.Revoke(revokedAt);

        token.IsRevoked.ShouldBeTrue();
        token.RevokedAtUtc.ShouldBe(revokedAt);
    }

    [Fact]
    public void Revoke_ShouldKeepFirstRevocationTime_WhenCalledTwice()
    {
        var token = CreateToken();
        var firstRevocation = CreatedAt.AddHours(1);

        token.Revoke(firstRevocation);
        token.Revoke(CreatedAt.AddHours(2));

        token.RevokedAtUtc.ShouldBe(firstRevocation);
    }

    [Fact]
    public void IsExpiredAt_ShouldReturnFalse_WhenTimeIsBeforeExpiry()
    {
        CreateToken().IsExpiredAt(ExpiresAt.AddSeconds(-1)).ShouldBeFalse();
    }

    [Fact]
    public void IsExpiredAt_ShouldReturnTrue_WhenTimeEqualsExpiry()
    {
        CreateToken().IsExpiredAt(ExpiresAt).ShouldBeTrue();
    }
}
