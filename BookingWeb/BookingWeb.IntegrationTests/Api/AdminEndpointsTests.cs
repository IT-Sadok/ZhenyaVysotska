using System.Net;
using System.Net.Http.Json;
using BookingWeb.Application.Models;
using BookingWeb.Domain;
using BookingWeb.IntegrationTests.Infrastructure;
using Shouldly;

namespace BookingWeb.IntegrationTests.Api;

public sealed class AdminEndpointsTests : ApiTestBase
{
    private const string UsersUrl = "/api/admin/users";

    public AdminEndpointsTests(ApiFactory factory) : base(factory)
    {
        
    }

    private async Task<HttpClient> CreateAdminClientAsync()
    {
        var admin = await LoginAsAdminAsync();
        return CreateAuthorizedClient(admin.AccessToken);
    }

    private static async Task<PagedResult<UserDto>> ReadPageAsync(HttpResponseMessage response)
    {
        await response.ShouldHaveStatusAsync(HttpStatusCode.OK);
        return (await response.Content.ReadFromJsonAsync<PagedResult<UserDto>>())!;
    }

    [Fact]
    public async Task GetUsers_ShouldReturnUnauthorized_WhenTokenIsMissing()
    {
        var response = await Client.GetAsync(UsersUrl);

        await response.ShouldHaveStatusAsync(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetUsers_ShouldReturnForbidden_WhenUserIsNotAdmin()
    {
        var host = await RegisterAsync("ivan@test.com", Roles.Host);
        using var httpClient = CreateAuthorizedClient(host.AccessToken);

        var response = await httpClient.GetAsync(UsersUrl);

        await response.ShouldHaveStatusAsync(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetUsers_ShouldReturnAllUsers_WhenFilterIsEmpty()
    {
        await RegisterAsync("host@test.com", Roles.Host);
        await RegisterAsync("client@test.com", Roles.Client);
        using var admin = await CreateAdminClientAsync();

        var page = await ReadPageAsync(await admin.GetAsync(UsersUrl));

        page.TotalCount.ShouldBe(3);
        page.Items.Select(user => user.Email).ShouldBe(
            new[] { ApiFactory.AdminEmail, "host@test.com", "client@test.com" }, ignoreOrder: true);
    }

    [Fact]
    public async Task GetUsers_ShouldReturnOnlyHosts_WhenRoleFilterIsHost()
    {
        await RegisterAsync("host@test.com", Roles.Host);
        await RegisterAsync("client@test.com", Roles.Client);
        using var admin = await CreateAdminClientAsync();

        var page = await ReadPageAsync(await admin.GetAsync($"{UsersUrl}?role=Host"));

        page.TotalCount.ShouldBe(1);
        page.Items.Single().Email.ShouldBe("host@test.com");
    }
    
    [Fact]
    public async Task GetUsers_ShouldReturnOnlyHosts_WhenRoleFilterHasDifferentCase()
    {
        await RegisterAsync("host@test.com", Roles.Host);
        await RegisterAsync("client@test.com", Roles.Client);
        using var admin = await CreateAdminClientAsync();

        var page = await ReadPageAsync(await admin.GetAsync($"{UsersUrl}?role=host"));

        page.TotalCount.ShouldBe(1);
        page.Items.Single().Email.ShouldBe("host@test.com");
    }

    [Fact]
    public async Task GetUsers_ShouldSortByEmailAscending_WhenSortByIsPlusEmail()
    {
        await RegisterAsync("charlie@test.com");
        await RegisterAsync("bravo@test.com");
        using var admin = await CreateAdminClientAsync();
        
        var sortBy = Uri.EscapeDataString("+email");
        var page = await ReadPageAsync(await admin.GetAsync($"{UsersUrl}?sortBy={sortBy}"));

        page.Items.Select(user => user.Email).ShouldBe(
            new[] { ApiFactory.AdminEmail, "bravo@test.com", "charlie@test.com" });
    }

    [Fact]
    public async Task GetUsers_ShouldReturnRequestedPage_WhenPageSizeIsSmallerThanTotal()
    {
        await RegisterAsync("charlie@test.com");
        await RegisterAsync("bravo@test.com");
        using var admin = await CreateAdminClientAsync();
        var sortBy = Uri.EscapeDataString("+email");

        var page = await ReadPageAsync(await admin.GetAsync($"{UsersUrl}?sortBy={sortBy}&page=2&pageSize=2"));

        page.TotalCount.ShouldBe(3);
        page.Page.ShouldBe(2);
        page.PageSize.ShouldBe(2);
        page.Items.Select(user => user.Email).ShouldBe(new[] { "charlie@test.com" });
    }

    [Fact]
    public async Task GetUsers_ShouldReturnValidationProblem_WhenPageSizeIsAbove100()
    {
        using var admin = await CreateAdminClientAsync();

        var response = await admin.GetAsync($"{UsersUrl}?pageSize=101");

        await response.ShouldHaveStatusAsync(HttpStatusCode.BadRequest);
        var fields = await response.ReadValidationErrorFieldsAsync();
        fields.ShouldContain(field => field.Equals("PageSize", StringComparison.OrdinalIgnoreCase));
    }
}
