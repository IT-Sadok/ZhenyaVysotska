using BookingWeb.Application.Auth;
using BookingWeb.Application.Auth.Requests;
using BookingWeb.Application.Auth.Validators;
using BookingWeb.Application.Interfaces;
using BookingWeb.Application.Models;
using BookingWeb.Application.Results;
using BookingWeb.Domain;
using FluentValidation;
using Moq;
using Shouldly;

namespace BookingWeb.UnitTests.Auth;

public sealed class AuthServiceTests
{
    private static readonly DateTime AccessTokenExpiresAtUtc = new(
        2026, 1, 15, 12, 15, 0, DateTimeKind.Utc);
    private static readonly DateTimeOffset RefreshTokenExpiresAtUtc = new(
        2026, 1, 22, 12, 0, 0, TimeSpan.Zero);

    private readonly Mock<IIdentityService> _identity = new();
    private readonly Mock<IJwtTokenGenerator> _jwt = new();
    private readonly Mock<IRefreshTokenService> _refreshTokens = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();

    private readonly AuthService _sut;

    public AuthServiceTests()
    {
        _jwt
            .Setup(jwt => jwt.GenerateToken(It.IsAny<Guid>(), It.IsAny<string>(), 
                It.IsAny<IEnumerable<string>>()))
            .Returns(new TokenResult("access-token", AccessTokenExpiresAtUtc));

        _refreshTokens
            .Setup(service => service.IssueAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RefreshTokenResult("refresh-token", RefreshTokenExpiresAtUtc));

        _sut = new AuthService(_identity.Object, _jwt.Object, _refreshTokens.Object, _unitOfWork.Object);
    }

    private static UserDto CreateUser(params string[] roles)
    {
        return new UserDto(Guid.NewGuid(), "user@example.com", roles);
    }

    private static RegisterRequest CreateRegisterRequest()
    {
        return new RegisterRequest(
            "user@example.com", "Passw0rd!", "Ivan", "Petrenko", Roles.Client);
    }

    [Fact]
    public async Task Register_ShouldReturnTokenPair_WhenIdentityCreatesUser()
    {
        _identity
            .Setup(identity => identity.RegisterAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateUser(Roles.Client));

        var result = await _sut.RegisterAsync(CreateRegisterRequest());

        result.IsSuccess.ShouldBeTrue();
        result.Value.AccessToken.ShouldBe("access-token");
        result.Value.RefreshToken.ShouldBe("refresh-token");
    }

    [Fact]
    public async Task Register_ShouldSaveRefreshToken_WhenIdentityCreatesUser()
    {
        _identity
            .Setup(identity => identity.RegisterAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateUser(Roles.Client));

        await _sut.RegisterAsync(CreateRegisterRequest());

        _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Register_ShouldReturnErrorWithoutIssuingTokens_WhenRegistrationFails()
    {
        _identity
            .Setup(identity => identity.RegisterAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(AuthErrors.EmailAlreadyUsed);

        var result = await _sut.RegisterAsync(CreateRegisterRequest());

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(AuthErrors.EmailAlreadyUsed);
        _refreshTokens.Verify(service => service.IssueAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Login_ShouldReturnTokenPair_WhenCredentialsAreValid()
    {
        _identity
            .Setup(identity => identity.ValidateCredentialsAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateUser(Roles.Client));

        var result = await _sut.LoginAsync(new LoginRequest("user@example.com", "Passw0rd!"));

        result.IsSuccess.ShouldBeTrue();
        result.Value.RefreshToken.ShouldBe("refresh-token");
    }

    [Fact]
    public async Task Login_ShouldReturnErrorWithoutIssuingTokens_WhenCredentialsAreWrong()
    {
        _identity
            .Setup(identity => identity.ValidateCredentialsAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(AuthErrors.InvalidCredentials);

        var result = await _sut.LoginAsync(new LoginRequest("user@example.com", "wrong-password"));

        result.Error.ShouldBe(AuthErrors.InvalidCredentials);
        _refreshTokens.Verify(service => service.IssueAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }
    
    [Fact]
    public async Task Refresh_ShouldRevokeOldTokenAndIssueNewOne_WhenTokenIsValid()
    {
        var user = CreateUser(Roles.Client);

        _refreshTokens
            .Setup(service => service.ValidateAsync("old-token", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user.Id);
        _identity
            .Setup(identity => identity.GetUserByIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var result = await _sut.RefreshAsync("old-token");

        result.IsSuccess.ShouldBeTrue();
        _refreshTokens.Verify(service => service.RevokeAsync("old-token", It.IsAny<CancellationToken>()), Times.Once);
        _refreshTokens.Verify(service => service.IssueAsync(user.Id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Refresh_ShouldSaveRotationOnce_WhenTokenIsValid()
    {
        var user = CreateUser(Roles.Client);

        _refreshTokens
            .Setup(service => service.ValidateAsync("old-token", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user.Id);
        _identity
            .Setup(identity => identity.GetUserByIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        await _sut.RefreshAsync("old-token");

        _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Refresh_ShouldReturnTokenErrorWithoutLoadingUser_WhenTokenIsInvalid()
    {
        _refreshTokens
            .Setup(service => service.ValidateAsync("bad-token", It.IsAny<CancellationToken>()))
            .ReturnsAsync(RefreshTokenErrors.Invalid);

        var result = await _sut.RefreshAsync("bad-token");

        result.Error.ShouldBe(RefreshTokenErrors.Invalid);
        _identity.Verify(identity => identity.GetUserByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        _refreshTokens.Verify(service => service.IssueAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Refresh_ShouldRevokeAllSessionsWithoutIssuingTokens_WhenUserNoLongerExists()
    {
        var userId = Guid.NewGuid();

        _refreshTokens
            .Setup(service => service.ValidateAsync("token", It.IsAny<CancellationToken>()))
            .ReturnsAsync(userId);
        _identity
            .Setup(identity => identity.GetUserByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(UserErrors.NotFound);

        var result = await _sut.RefreshAsync("token");

        result.IsFailure.ShouldBeTrue();
        _refreshTokens.Verify(service => service.RevokeAllForUserAsync(userId, It.IsAny<CancellationToken>()), Times.Once);
        _refreshTokens.Verify(service => service.IssueAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Logout_ShouldRevokeTokenAndSaveChanges()
    {
        await _sut.LogoutAsync("token");

        _refreshTokens.Verify(service => service.RevokeAsync("token", It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AddRole_ShouldPassCanonicalRoleName_WhenRoleIsInDifferentCase()
    {
        var user = CreateUser(Roles.Client, Roles.Host);

        _identity
            .Setup(identity => identity.AddToRoleAsync(user.Id, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        await _sut.AddRoleAsync(user.Id, new AddRoleRequest("host"));

        _identity.Verify(identity => identity.AddToRoleAsync(user.Id, Roles.Host, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AddRole_ShouldIssueAccessTokenWithUpdatedRoles_WhenRoleIsAdded()
    {
        var user = CreateUser(Roles.Client, Roles.Host);

        _identity
            .Setup(identity => identity.AddToRoleAsync(user.Id, Roles.Host, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var result = await _sut.AddRoleAsync(user.Id, new AddRoleRequest(Roles.Host));

        result.IsSuccess.ShouldBeTrue();
        result.Value.AccessToken.ShouldBe("access-token");
        _jwt.Verify(
            jwt => jwt.GenerateToken(user.Id, user.Email, It.Is<IEnumerable<string>>(r => r.Contains(Roles.Host))),
            Times.Once);
    }

    [Fact]
    public async Task AddRole_ShouldNotTouchRefreshTokens_WhenRoleIsAdded()
    {
        var user = CreateUser(Roles.Client, Roles.Host);

        _identity
            .Setup(identity => identity.AddToRoleAsync(user.Id, Roles.Host, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        await _sut.AddRoleAsync(user.Id, new AddRoleRequest(Roles.Host));

        _refreshTokens.Verify(service => service.IssueAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.Verify(unitOfWork => unitOfWork.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AddRole_ShouldReturnErrorWithoutIssuingToken_WhenUserIsNotFound()
    {
        var userId = Guid.NewGuid();

        _identity
            .Setup(identity => identity.AddToRoleAsync(userId, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(UserErrors.NotFound);

        var result = await _sut.AddRoleAsync(userId, new AddRoleRequest(Roles.Host));

        result.Error.ShouldBe(UserErrors.NotFound);
        _jwt.Verify(
            jwt => jwt.GenerateToken(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<IEnumerable<string>>()),
            Times.Never);
    }
}
