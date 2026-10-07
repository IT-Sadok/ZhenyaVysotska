using BookingWeb.Application.Results;

namespace BookingWeb.Application.Auth;

public static class RefreshTokenErrors
{
    public static readonly Error Invalid =
        new("Auth.RefreshInvalid", "Refresh token is invalid.", ErrorType.Unauthorized);
 
    public static readonly Error Reused =
        new("Auth.RefreshReused", "Refresh token was already used. All sessions have been revoked.", ErrorType.Unauthorized);
 
    public static readonly Error Expired =
        new("Auth.RefreshExpired", "Refresh token has expired.", ErrorType.Unauthorized);
}