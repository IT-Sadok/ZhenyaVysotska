using System.Reflection;
using BookingWeb.Application.Admin;
using BookingWeb.Application.Auth;
using BookingWeb.Application.Profiles;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace BookingWeb.Application;

public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
        services.AddScoped<AuthService>();
        services.AddScoped<AdminService>();
        services.AddScoped<ProfileService>();
        return services;
    }
}