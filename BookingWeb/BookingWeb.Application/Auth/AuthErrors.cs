using BookingWeb.Application.Results;

namespace BookingWeb.Application.Auth;

public static class AuthErrors
{
    public static readonly Error EmailAlreadyUsed =
        new("Auth.EmailAlreadyUsed", "User with this email already exists");
    
    public static readonly Error InvalidCredentials =
        new("Auth.InvalidCredentials", "Invalid email or password");
}