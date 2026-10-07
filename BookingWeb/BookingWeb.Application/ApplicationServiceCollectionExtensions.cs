using System.Reflection;
using BookingWeb.Application.Admin;
using BookingWeb.Application.Auth;
using BookingWeb.Application.Interfaces;
using BookingWeb.Application.Profiles;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace BookingWeb.Application;

public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IAdminService, AdminService>();
        services.AddScoped<IProfileService, ProfileService>();
        services.AddScoped<IRefreshTokenService, RefreshTokenService>(); 
        
        services.TryAddSingleton(TimeProvider.System);
        return services;
    }
}