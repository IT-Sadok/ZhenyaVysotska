namespace BookingWeb.Application.Constants;

public static class SortTokens
{
    public const string EmailAsc  = "+email";
    public const string EmailDesc = "-email";
    public static readonly IReadOnlyList<string> All = new[] { EmailAsc, EmailDesc };
}