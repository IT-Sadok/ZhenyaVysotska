using BookingWeb.Application.Constants;
using BookingWeb.Application.Models;
using BookingWeb.Domain;
using BookingWeb.Domain.Models;
using BookingWeb.Infrastructure.Identity;
using BookingWeb.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace BookingWeb.IntegrationTests;

[Collection(nameof(PostgresCollection))]
public sealed class UserReadServiceTests
{
    private readonly PostgresContainerFixture _fixture;

    public UserReadServiceTests(PostgresContainerFixture fixture) => _fixture = fixture;
    
    private async Task ResetAndSeedAsync()
    {
        await using var db = _fixture.CreateContext();
        
        await db.Database.ExecuteSqlRawAsync(
            """TRUNCATE "AspNetUserRoles", "AspNetUsers", "AspNetRoles", "UserProfiles", "RefreshTokens" RESTART IDENTITY CASCADE;""");

        var clientRoleId = Guid.NewGuid();
        var hostRoleId = Guid.NewGuid();
        var adminRoleId = Guid.NewGuid();

        db.Roles.AddRange(
            Role(clientRoleId, Roles.Client),
            Role(hostRoleId, Roles.Host),
            Role(adminRoleId, Roles.Admin));

        var alice = User("alice@example.com", Roles.Client);
        var bob = User("bob@example.com", Roles.Host);
        var oksana = User("oksana@example.com", Roles.Host);  
        var carol = User("CAROL@mail.test", Roles.Client);

        db.Users.AddRange(alice, bob, oksana, carol);

        db.UserRoles.AddRange(
            Link(alice.Id, clientRoleId),
            Link(bob.Id, hostRoleId),
            Link(oksana.Id, clientRoleId),
            Link(oksana.Id, hostRoleId),
            Link(carol.Id, clientRoleId));

        db.UserProfiles.AddRange(
            UserProfile.Create(alice.Id, "Alice", "A"),
            UserProfile.Create(bob.Id, "Bob", "B"),
            UserProfile.Create(oksana.Id, "Oksana", "O"),
            UserProfile.Create(carol.Id, "Carol", "C"));

        await db.SaveChangesAsync();
    }

    private static IdentityRole<Guid> Role(Guid id, string name) => new()
    {
        Id = id,
        Name = name,
        NormalizedName = name.ToUpperInvariant()
    };

    private static ApplicationUser User(string email, string persona) => new()
    {
        Id = Guid.NewGuid(),
        Email = email,
        UserName = email,
        NormalizedEmail = email.ToUpperInvariant(),
        NormalizedUserName = email.ToUpperInvariant(),
        DefaultPersona = persona,
        SecurityStamp = Guid.NewGuid().ToString()
    };

    private static IdentityUserRole<Guid> Link(Guid userId, Guid roleId) => new()
    {
        UserId = userId,
        RoleId = roleId
    };
  
    private UserReadService CreateSut(ApplicationDbContext db) => new(db);
    
    [Fact]
    public async Task Filter_by_host_returns_only_hosts_without_duplicating_multi_role_user()
    {
        await ResetAndSeedAsync();
        await using var db = _fixture.CreateContext();

        var result = await CreateSut(db).GetUsersAsync(
            new UserFilterDto(Role: Roles.Host, Email: null, SortBy: null), CancellationToken.None);
        
        result.Items.Count.ShouldBe(2);
        result.Items.Select(u => u.Id).Distinct().Count().ShouldBe(2);
        result.Items.Select(u => u.Email)
            .ShouldBe(new[] { "bob@example.com", "oksana@example.com" }, ignoreOrder: true);
    }

    [Fact]
    public async Task Filter_by_client_returns_all_clients()
    {
        await ResetAndSeedAsync();
        await using var db = _fixture.CreateContext();

        var result = await CreateSut(db).GetUsersAsync(
            new UserFilterDto(Role: Roles.Client, Email: null, SortBy: null), CancellationToken.None);

        result.Items.Select(u => u.Email)
            .ShouldBe(new[] { "alice@example.com", "oksana@example.com", "CAROL@mail.test" }, ignoreOrder: true);
    }

    [Fact]
    public async Task Multi_role_user_carries_both_roles_in_dto()
    {
        await ResetAndSeedAsync();
        await using var db = _fixture.CreateContext();

        var result = await CreateSut(db).GetUsersAsync(
            new UserFilterDto(Role: Roles.Host, Email: "oksana", SortBy: null), CancellationToken.None);

        var oksana = result.Items.ShouldHaveSingleItem();
        oksana.Roles.ShouldBe(new[] { Roles.Client, Roles.Host }, ignoreOrder: true);
    }
    

    [Fact]
    public async Task Email_contains_is_case_sensitive_lowercase_query_misses_uppercase_email()
    {
        await ResetAndSeedAsync();
        await using var db = _fixture.CreateContext();
        
        var result = await CreateSut(db).GetUsersAsync(
            new UserFilterDto(Role: null, Email: "carol", SortBy: null), CancellationToken.None);

        result.Items.ShouldBeEmpty();
    }

    [Fact]
    public async Task Email_contains_matches_same_case()
    {
        await ResetAndSeedAsync();
        await using var db = _fixture.CreateContext();

        var result = await CreateSut(db).GetUsersAsync(
            new UserFilterDto(Role: null, Email: "CAROL", SortBy: null), CancellationToken.None);

        result.Items.ShouldHaveSingleItem().Email.ShouldBe("CAROL@mail.test");
    }
    
    [Fact]
    public async Task Sort_by_email_asc()
    {
        await ResetAndSeedAsync();
        await using var db = _fixture.CreateContext();

        var result = await CreateSut(db).GetUsersAsync(
            new UserFilterDto(Role: null, Email: "example.com", SortBy: SortTokens.EmailAsc), CancellationToken.None);

        result.Items.Select(u => u.Email)
            .ShouldBe(new[] { "alice@example.com", "bob@example.com", "oksana@example.com" });
    }

    [Fact]
    public async Task Sort_by_email_desc()
    {
        await ResetAndSeedAsync();
        await using var db = _fixture.CreateContext();

        var result = await CreateSut(db).GetUsersAsync(
            new UserFilterDto(Role: null, Email: "example.com", SortBy: SortTokens.EmailDesc), CancellationToken.None);

        result.Items.Select(u => u.Email)
            .ShouldBe(new[] { "oksana@example.com", "bob@example.com", "alice@example.com" });
    }
    
    [Fact]
    public async Task Pagination_slices_pages_without_overlap()
    {
        await ResetAndSeedAsync();
        await using var db = _fixture.CreateContext();
        var sut = CreateSut(db);

        var page1 = await sut.GetUsersAsync(
            new UserFilterDto(null, "example.com", SortTokens.EmailAsc, Page: 1, PageSize: 2), CancellationToken.None);
        var page2 = await sut.GetUsersAsync(
            new UserFilterDto(null, "example.com", SortTokens.EmailAsc, Page: 2, PageSize: 2), CancellationToken.None);

        page1.Items.Select(u => u.Email).ShouldBe(new[] { "alice@example.com", "bob@example.com" });
        page2.Items.Select(u => u.Email).ShouldBe(new[] { "oksana@example.com" });

        page1.Items.Select(u => u.Id).Intersect(page2.Items.Select(u => u.Id)).ShouldBeEmpty();
    }
    
    [Fact]
    public async Task No_filter_returns_all_seeded_users()
    {
        await ResetAndSeedAsync();
        await using var db = _fixture.CreateContext();

        var result = await CreateSut(db).GetUsersAsync(
            new UserFilterDto(Role: null, Email: null, SortBy: null), CancellationToken.None);

        result.Items.Count.ShouldBe(4); 
    }
}