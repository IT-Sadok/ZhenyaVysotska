using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using BookingWeb.Application.Auth.Requests;
using BookingWeb.Application.Auth.Responses;
using BookingWeb.Application.Models;
using BookingWeb.Domain;
using BookingWeb.Infrastructure.Persistence;
using BookingWeb.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace BookingWeb.IntegrationTests.Api;

public sealed class AuthEndpointsTests : ApiTestBase
{
    public AuthEndpointsTests(ApiFactory factory) : base(factory)
    {
        
    }

    [Fact]
    public async Task Register_ShouldReturnTokens_WhenRequestIsValid()
    {
        var auth = await RegisterAsync("ivan@test.com", Roles.Client);

        auth.AccessToken.ShouldNotBeNullOrWhiteSpace();
        auth.RefreshToken.ShouldNotBeNullOrWhiteSpace();
        TestJwt.ReadRoles(auth.AccessToken).ShouldBe(new[] { Roles.Client });
    }

    [Fact]
    public async Task Register_ShouldReturnValidationProblem_WhenEmailIsInvalid()
    {
        var request = new RegisterRequest("not-an-email", DefaultPassword, "Ivan", "Petrenko", Roles.Client);

        var response = await Client.PostAsJsonAsync("/api/auth/register", request);

        await response.ShouldHaveStatusAsync(HttpStatusCode.BadRequest);
        var fields = await response.ReadValidationErrorFieldsAsync();
        fields.ShouldContain(field => field.Equals("Email", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Register_ShouldReturnConflict_WhenEmailIsAlreadyUsed()
    {
        await RegisterAsync("ivan@test.com");
        var request = new RegisterRequest("ivan@test.com", DefaultPassword, "Other", "Person", Roles.Client);

        var response = await Client.PostAsJsonAsync("/api/auth/register", request);

        await response.ShouldHaveStatusAsync(HttpStatusCode.Conflict);
        (await response.ReadProblemTitleAsync()).ShouldBe("Auth.EmailAlreadyUsed");
    }
    
    [Fact]
    public async Task Register_ShouldStoreOnlyRefreshTokenHash_WhenUserRegisters()
    {
        var auth = await RegisterAsync("ivan@test.com");

        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var storedHashes = await db.RefreshTokens.Select(token => token.TokenHash).ToListAsync();

        var expectedHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(auth.RefreshToken)));
        storedHashes.ShouldBe(new[] { expectedHash });
        storedHashes.ShouldNotContain(auth.RefreshToken);
    }
    
    [Fact]
    public async Task Login_ShouldReturnTokens_WhenCredentialsAreValid()
    {
        await RegisterAsync("ivan@test.com", Roles.Host);

        var auth = await LoginAsync("ivan@test.com");

        auth.AccessToken.ShouldNotBeNullOrWhiteSpace();
        TestJwt.ReadRoles(auth.AccessToken).ShouldBe(new[] { Roles.Host });
    }

    [Fact]
    public async Task Login_ShouldReturnUnauthorized_WhenPasswordIsWrong()
    {
        await RegisterAsync("ivan@test.com");

        var response = await Client.PostAsJsonAsync(
            "/api/auth/login", new LoginRequest("ivan@test.com", "WrongPassword1!"));

        await response.ShouldHaveStatusAsync(HttpStatusCode.Unauthorized);
        (await response.ReadProblemTitleAsync()).ShouldBe("Auth.InvalidCredentials");
    }

    [Fact]
    public async Task Login_ShouldReturnSameUnauthorized_WhenEmailIsUnknown()
    {
        var response = await Client.PostAsJsonAsync(
            "/api/auth/login", new LoginRequest("nobody@test.com", DefaultPassword));

        await response.ShouldHaveStatusAsync(HttpStatusCode.Unauthorized);
        (await response.ReadProblemTitleAsync()).ShouldBe("Auth.InvalidCredentials");
    }
    
    [Fact]
    public async Task GetMe_ShouldReturnCurrentUser_WhenTokenIsValid()
    {
        var auth = await RegisterAsync("ivan@test.com", Roles.Host);
        using var client = CreateAuthorizedClient(auth.AccessToken);

        var me = await client.GetFromJsonAsync<UserDto>("/api/auth/me");

        me.ShouldNotBeNull();
        me.Email.ShouldBe("ivan@test.com");
        me.Roles.ShouldBe(new[] { Roles.Host });
    }

    [Fact]
    public async Task GetMe_ShouldReturnUnauthorized_WhenTokenIsMissing()
    {
        var response = await Client.GetAsync("/api/auth/me");

        await response.ShouldHaveStatusAsync(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetMe_ShouldReturnUnauthorized_WhenTokenSignatureIsInvalid()
    {
        var auth = await RegisterAsync("ivan@test.com");
        var parts = auth.AccessToken.Split('.');
        var forgedToken = $"{parts[0]}.{parts[1]}.{new string(parts[2].Reverse().ToArray())}";
        using var client = CreateAuthorizedClient(forgedToken);

        var response = await client.GetAsync("/api/auth/me");

        await response.ShouldHaveStatusAsync(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task AddRole_ShouldReturnTokenWithBothRoles_WhenClientAddsHost()
    {
        var auth = await RegisterAsync("ivan@test.com", Roles.Client);
        using var client = CreateAuthorizedClient(auth.AccessToken);
        
        var response = await client.PutAsync("/api/auth/me/roles/host", content: null);

        await response.ShouldHaveStatusAsync(HttpStatusCode.OK);
        var newToken = (await response.Content.ReadFromJsonAsync<AccessTokenResponse>())!;
        TestJwt.ReadRoles(newToken.AccessToken).ShouldBe(new[] { Roles.Client, Roles.Host }, ignoreOrder: true);

        var me = await client.GetFromJsonAsync<UserDto>("/api/auth/me");
        me!.Roles.ShouldBe(new[] { Roles.Client, Roles.Host }, ignoreOrder: true);
    }

    [Fact]
    public async Task AddRole_ShouldNotDuplicateRole_WhenRoleIsAlreadyAssigned()
    {
        var auth = await RegisterAsync("ivan@test.com", Roles.Host);
        using var client = CreateAuthorizedClient(auth.AccessToken);

        var response = await client.PutAsync("/api/auth/me/roles/Host", content: null);

        await response.ShouldHaveStatusAsync(HttpStatusCode.OK);
        var me = await client.GetFromJsonAsync<UserDto>("/api/auth/me");
        me!.Roles.ShouldBe(new[] { Roles.Host });
    }

    [Fact]
    public async Task AddRole_ShouldReturnValidationProblemAndNotGrantRole_WhenRoleIsAdmin()
    {
        var auth = await RegisterAsync("ivan@test.com");
        using var client = CreateAuthorizedClient(auth.AccessToken);

        var response = await client.PutAsync("/api/auth/me/roles/Admin", content: null);

        await response.ShouldHaveStatusAsync(HttpStatusCode.BadRequest);
        var me = await client.GetFromJsonAsync<UserDto>("/api/auth/me");
        me!.Roles.ShouldNotContain(Roles.Admin);
    }

    [Fact]
    public async Task AddRole_ShouldReturnUnauthorized_WhenTokenIsMissing()
    {
        var response = await Client.PutAsync("/api/auth/me/roles/Host", content: null);

        await response.ShouldHaveStatusAsync(HttpStatusCode.Unauthorized);
    }
    
     private Task<HttpResponseMessage> RefreshAsync(string refreshToken)
    {
        return Client.PostAsJsonAsync("/api/auth/refresh", new RefreshRequest(refreshToken));
    }
     
    [Fact]
    public async Task Refresh_ShouldReturnWorkingNewTokenPair_WhenRefreshTokenIsValid()
    {
        var auth = await RegisterAsync("ivan@test.com");

        var response = await RefreshAsync(auth.RefreshToken);

        await response.ShouldHaveStatusAsync(HttpStatusCode.OK);
        var refreshed = (await response.Content.ReadFromJsonAsync<AuthResponse>())!;
        refreshed.RefreshToken.ShouldNotBe(auth.RefreshToken);

        using var client = CreateAuthorizedClient(refreshed.AccessToken);
        var meResponse = await client.GetAsync("/api/auth/me");
        await meResponse.ShouldHaveStatusAsync(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Refresh_ShouldReturnUnauthorized_WhenTokenIsUnknown()
    {
        var response = await RefreshAsync("definitely-not-a-real-refresh-token");

        await response.ShouldHaveStatusAsync(HttpStatusCode.Unauthorized);
        (await response.ReadProblemTitleAsync()).ShouldBe("Auth.RefreshInvalid");
    }

    [Fact]
    public async Task Refresh_ShouldReturnUnauthorized_WhenTokenWasAlreadyRotated()
    {
        var auth = await RegisterAsync("ivan@test.com");
        (await RefreshAsync(auth.RefreshToken)).EnsureSuccessStatusCode();

        var reuseResponse = await RefreshAsync(auth.RefreshToken);

        await reuseResponse.ShouldHaveStatusAsync(HttpStatusCode.Unauthorized);
        (await reuseResponse.ReadProblemTitleAsync()).ShouldBe("Auth.RefreshReused");
    }
    
    [Fact]
    public async Task Refresh_ShouldRevokeAllUserSessions_WhenRotatedTokenIsReused()
    {
        var sessionA = await RegisterAsync("ivan@test.com");
        var sessionB = await LoginAsync("ivan@test.com");

        var rotatedResponse = await RefreshAsync(sessionA.RefreshToken);
        await rotatedResponse.ShouldHaveStatusAsync(HttpStatusCode.OK);
        var rotatedA = (await rotatedResponse.Content.ReadFromJsonAsync<AuthResponse>())!;

        await (await RefreshAsync(sessionA.RefreshToken)).ShouldHaveStatusAsync(HttpStatusCode.Unauthorized);
        
        await (await RefreshAsync(rotatedA.RefreshToken)).ShouldHaveStatusAsync(HttpStatusCode.Unauthorized);
        await (await RefreshAsync(sessionB.RefreshToken)).ShouldHaveStatusAsync(HttpStatusCode.Unauthorized);
    }
    
    [Fact]
    public async Task Logout_ShouldRevokeRefreshToken_WhenUserIsAuthenticated()
    {
        var auth = await RegisterAsync("ivan@test.com");
        using var client = CreateAuthorizedClient(auth.AccessToken);

        var logoutResponse = await client.PostAsJsonAsync("/api/auth/logout", new RefreshRequest(auth.RefreshToken));

        await logoutResponse.ShouldHaveStatusAsync(HttpStatusCode.NoContent);
        await (await RefreshAsync(auth.RefreshToken)).ShouldHaveStatusAsync(HttpStatusCode.Unauthorized);
    }
    
    [Fact]
    public async Task Logout_ShouldReturnUnauthorized_WhenAccessTokenIsMissing()
    {
        var auth = await RegisterAsync("ivan@test.com");

        var response = await Client.PostAsJsonAsync("/api/auth/logout", new RefreshRequest(auth.RefreshToken));

        await response.ShouldHaveStatusAsync(HttpStatusCode.Unauthorized);
        await (await RefreshAsync(auth.RefreshToken)).ShouldHaveStatusAsync(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Logout_ShouldNotInvalidateAccessToken_WhenAccessTokenIsNotExpired()
    {
        var auth = await RegisterAsync("ivan@test.com");
        using var client = CreateAuthorizedClient(auth.AccessToken);
        (await client.PostAsJsonAsync("/api/auth/logout", new RefreshRequest(auth.RefreshToken)))
            .EnsureSuccessStatusCode();

        var meResponse = await client.GetAsync("/api/auth/me");

        await meResponse.ShouldHaveStatusAsync(HttpStatusCode.OK);
    }
}
