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

public class AuthServiceTests
{
    private readonly Mock<IIdentityService> _identity = new();
    private readonly Mock<IJwtTokenGenerator> _jwt = new();
    private readonly Mock<IRefreshTokenService> _refresh = new();

    private AuthService CreateSut() => new(
        _identity.Object,
        _jwt.Object,
        _refresh.Object,
        new IValidator<RegisterRequest>[] { new RegisterRequestValidator() },
        new IValidator<LoginRequest>[] { new LoginRequestValidator() });

    private static UserDto UserWith(params string[] roles) =>
        new(Guid.NewGuid(), "user@example.com", roles, roles.FirstOrDefault());
    
    private static UserDto UserWithId(Guid id, params string[] roles) =>
        new(id, "user@example.com", roles, roles.FirstOrDefault());

    private void SetupTokenPair()
    {
        _jwt.Setup(j => j.GenerateToken(It.IsAny<Guid>(), It.IsAny<string>(),
                It.IsAny<IEnumerable<string>>(), It.IsAny<string>()))
            .Returns(new TokenResult("access-token", DateTime.UtcNow.AddMinutes(60)));
        _refresh.Setup(r => r.IssueAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RefreshTokenResult("refresh-token", DateTimeOffset.UtcNow.AddDays(7)));
    }

    private static RegisterRequest ValidRegister() =>
        new("user@example.com", "Passw0rd!", "Ivan", "Paliychuk", Roles.Client);
    
    [Fact]
    public async Task Register_success_returns_token_pair()
    {
        _identity.Setup(i => i.RegisterAsync(
                It.IsAny<string>(), It.IsAny<string>(), 
                It.IsAny<string>(), It.IsAny<string>(), 
                It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(UserWith(Roles.Client)));
        SetupTokenPair();

        var result = await CreateSut().RegisterAsync(ValidRegister());

        result.IsSuccess.ShouldBeTrue();
        result.Value.AccessToken.ShouldBe("access-token");
        result.Value.RefreshToken.ShouldBe("refresh-token");
    }

    [Fact]
    public async Task Register_invalid_input_skips_identity()
    {
        var badRequest = ValidRegister() with { Email = "not-an-email" };

        var result = await CreateSut().RegisterAsync(badRequest);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBeOfType<ValidationError>();
        _identity.Verify(i => i.RegisterAsync(
                It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<string>(), 
                It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never); 
    }

    [Fact]
    public async Task Register_taken_email_issues_no_token()
    {
        _identity.Setup(i => i.RegisterAsync(
                It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), 
                It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<UserDto>(AuthErrors.EmailAlreadyUsed));

        var result = await CreateSut().RegisterAsync(ValidRegister());

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe(AuthErrors.EmailAlreadyUsed.Code);
        _refresh.Verify(r => r.IssueAsync(
            It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    
    [Fact]
    public async Task Login_success_returns_token_pair()
    {
        _identity.Setup(i => i.ValidateCredentialsAsync(
                It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(UserWith(Roles.Client)));
        SetupTokenPair();

        var result = await CreateSut().LoginAsync(new LoginRequest("user@example.com", "Passw0rd!"));

        result.IsSuccess.ShouldBeTrue();
        result.Value.RefreshToken.ShouldBe("refresh-token");
    }

    [Fact]
    public async Task Login_bad_credentials_issues_no_token()
    {
        _identity.Setup(i => i.ValidateCredentialsAsync(It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<UserDto>(AuthErrors.InvalidCredentials));

        var result = await CreateSut().LoginAsync(new LoginRequest("user@example.com", "wrongpass"));

        result.IsFailure.ShouldBeTrue();
        _refresh.Verify(r => r.IssueAsync(
            It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Login_invalid_input_skips_identity()
    {
        var result = await CreateSut().LoginAsync(new LoginRequest("", ""));

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBeOfType<ValidationError>();
        _identity.Verify(i => i.ValidateCredentialsAsync(It.IsAny<string>(), It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    
    [Fact]
    public async Task EnablePersona_adds_role_and_switches()
    {
        var userId = Guid.NewGuid();
        _identity.Setup(i => i.AddToRoleAsync(
                userId, Roles.Host, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(UserWith(Roles.Client, Roles.Host)));
        _identity.Setup(i => i.SetDefaultPersonaAsync(
                userId, Roles.Host, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(UserWith(Roles.Client, Roles.Host)));
        SetupTokenPair();

        var result = await CreateSut().EnablePersonaAsync(userId, Roles.Host);

        result.IsSuccess.ShouldBeTrue();
        result.Value.AccessToken.ShouldBe("access-token");
        _identity.Verify(i => i.AddToRoleAsync(
            userId, Roles.Host, It.IsAny<CancellationToken>()), Times.Once);
        _identity.Verify(i => i.SetDefaultPersonaAsync(
            userId, Roles.Host, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task EnablePersona_admin_is_rejected_before_identity()
    {
        var result = await CreateSut().EnablePersonaAsync(Guid.NewGuid(), Roles.Admin);

        result.IsFailure.ShouldBeTrue();
        _identity.Verify(i => i.AddToRoleAsync(
            It.IsAny<Guid>(), It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Never); 
    }

    [Fact]
    public async Task EnablePersona_when_add_role_fails_does_not_switch()
    {
        var userId = Guid.NewGuid();
        _identity.Setup(i => i.AddToRoleAsync(
                userId, Roles.Host, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<UserDto>(new Error("User.NotFound", "not found")));

        var result = await CreateSut().EnablePersonaAsync(userId, Roles.Host);

        result.IsFailure.ShouldBeTrue();
        _identity.Verify(i => i.SetDefaultPersonaAsync(
            It.IsAny<Guid>(), It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SwitchPersona_success_returns_access_token()
    {
        var userId = Guid.NewGuid();
        _identity.Setup(i => i.SetDefaultPersonaAsync(
                userId, Roles.Host, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(UserWith( Roles.Client, Roles.Host)));
        SetupTokenPair();

        var result = await CreateSut().SwitchPersonaAsync(userId, Roles.Host);

        result.IsSuccess.ShouldBeTrue();
        result.Value.AccessToken.ShouldBe("access-token");
    }

    [Fact]
    public async Task SwitchPersona_not_owned_issues_no_token()
    {
        var userId = Guid.NewGuid();
        _identity.Setup(i => i.SetDefaultPersonaAsync(
                userId, Roles.Host, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<UserDto>(new Error("Persona.NotOwned", "not owned")));

        var result = await CreateSut().SwitchPersonaAsync(userId, Roles.Host);

        result.IsFailure.ShouldBeTrue();
        _jwt.Verify(j => j.GenerateToken(It.IsAny<Guid>(), It.IsAny<string>(),
            It.IsAny<IEnumerable<string>>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Refresh_valid_rotates_old_and_issues_new()
    {
        var userId = Guid.NewGuid();
        _refresh.Setup(r => r.ValidateAsync(
                "old-token", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(userId));
        _identity.Setup(i => i.GetActiveUserAsync(
                userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(UserWithId(userId, Roles.Client)));
        SetupTokenPair();

        var result = await CreateSut().RefreshAsync("old-token");

        result.IsSuccess.ShouldBeTrue();
        _refresh.Verify(r => r.RevokeAsync("old-token", It.IsAny<CancellationToken>()), Times.Once); 
        _refresh.Verify(r => r.IssueAsync(userId, It.IsAny<CancellationToken>()), Times.Once);    
    }

    [Fact]
    public async Task Refresh_invalid_token_issues_no_pair()
    {
        _refresh.Setup(r => r.ValidateAsync("reused", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<Guid>(new Error("Auth.RefreshReused", "token is reused")));

        var result = await CreateSut().RefreshAsync("reused");

        result.IsFailure.ShouldBeTrue();
        _identity.Verify(i => i.GetActiveUserAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        _refresh.Verify(r => r.IssueAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Refresh_when_user_inactive_revokes_all_sessions()
    {
        var userId = Guid.NewGuid();
        _refresh.Setup(r => r.ValidateAsync("valid", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(userId));
   
        _identity.Setup(i => i.GetActiveUserAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<UserDto>(new Error("User.NotFound", "not found")));

        var result = await CreateSut().RefreshAsync("valid");

        result.IsFailure.ShouldBeTrue();
        _refresh.Verify(r => r.RevokeAllForUserAsync(userId, It.IsAny<CancellationToken>()), Times.Once);
        _refresh.Verify(r => r.IssueAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
