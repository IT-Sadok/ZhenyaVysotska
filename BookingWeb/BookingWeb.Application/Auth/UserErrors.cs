using BookingWeb.Application.Results;

namespace BookingWeb.Application.Auth;

public static class UserErrors
{
    public static readonly Error NotFound =
        new("User.NotFound", "User is not found.", ErrorType.NotFound);
    
    public static Error RegistrationFailed(string details) =>
        new("User.RegistrationFailed", details);
 
    public static Error RoleAssignmentFailed(string details) =>
        new("User.RoleAssignmentFailed", details, ErrorType.Conflict);
}