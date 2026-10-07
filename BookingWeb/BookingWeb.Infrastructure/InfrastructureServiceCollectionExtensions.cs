using System.Security.Claims;
using System.Text;
using BookingWeb.Application.Common.Settings;
using BookingWeb.Application.Constants;
using BookingWeb.Application.Interfaces;
using BookingWeb.Domain;
using BookingWeb.Infrastructure.Authentication;
using BookingWeb.Infrastructure.Identity;
using BookingWeb.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace BookingWeb.Infrastructure;

public static class InfrastructureServiceCollectionExtensions
{
    private const int MinJwtSecretLengthInBytes = 32;
 
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.AddPersistence(configuration);
        services.AddIdentityServices();
        services.AddTokenServices(configuration);
        services.AddAuthenticationAndAuthorization(configuration);
 
        return services;
    }
 
    private static void AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException(
                "Connection string 'Default' is not configured. ");
 
        services.AddDbContext<ApplicationDbContext>(options => options.UseNpgsql(connectionString));
        
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IUserProfileRepository, UserProfileRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
    }
 
    private static void AddIdentityServices(this IServiceCollection services)
    {
        services.AddIdentityCore<ApplicationUser>(options =>
            {
                options.Password.RequiredLength = 8;
                options.User.RequireUniqueEmail = true;
                options.Password.RequireNonAlphanumeric = false;
            })
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<ApplicationDbContext>();
 
        services.AddScoped<IIdentityService, IdentityService>();
    }
 
    private static void AddTokenServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));
        
        services.Configure<RefreshTokenSettings>(configuration.GetSection(RefreshTokenSettings.SectionName));
 
        services.AddSingleton<IJwtTokenGenerator, JwtTokenGenerator>();
    }
 
    private static void AddAuthenticationAndAuthorization(
        this IServiceCollection services, IConfiguration configuration)
    {
        var jwtSettings = GetValidatedJwtSettings(configuration);
 
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.MapInboundClaims = false;
 
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtSettings.Issuer,
                    ValidAudience = jwtSettings.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Secret)),
                    RoleClaimType = "role",
                    NameClaimType = "sub"
                };
            });
 
        services.AddAuthorizationBuilder()
            .AddPolicy(PolicyNames.AdminOnly, policy => policy.RequireRole(Roles.Admin))
            .AddPolicy(PolicyNames.HostOnly, policy => policy.RequireRole(Roles.Host));
    }
 

    private static JwtSettings GetValidatedJwtSettings(IConfiguration configuration)
    {
        var jwtSettings = configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>()
            ?? throw new InvalidOperationException(
                $"Configuration section '{JwtSettings.SectionName}' is missing.");
 
        if (string.IsNullOrWhiteSpace(jwtSettings.Secret))
        {
            throw new InvalidOperationException(
                $"'{JwtSettings.SectionName}:Secret' is not configured. Set it in user-secrets.");
        }
 
        if (Encoding.UTF8.GetByteCount(jwtSettings.Secret) < MinJwtSecretLengthInBytes)
        {
            throw new InvalidOperationException(
                $"'{JwtSettings.SectionName}:Secret' must be at least {MinJwtSecretLengthInBytes} bytes for HS256.");
        }
 
        return jwtSettings;
    }
}