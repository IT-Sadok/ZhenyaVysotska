using BookingWeb.Application.Results;

namespace BookingWeb.Application.Accounts;

public static class AccountErrors
{
    public static readonly Error AdminCannotBeDeleted =
        new("Account.AdminCannotBeDeleted", "Administrator accounts can't be deleted.", ErrorType.Forbidden);

    public static readonly Error InvalidPassword =
        new("Account.InvalidPassword", "Password is incorrect.");

    public static readonly Error HasUpcomingBookingsAsGuest =
        new("Account.HasUpcomingBookingsAsGuest",
            "Cancel your upcoming bookings before deleting the account.",
            ErrorType.Conflict);

    public static readonly Error HasUpcomingBookingsAsHost =
        new("Account.HasUpcomingBookingsAsHost",
            "Your listings have upcoming bookings. Cancel them before deleting the account.",
            ErrorType.Conflict);

    public static Error DeletionFailed(string details) =>
        new("Account.DeletionFailed", details);
}