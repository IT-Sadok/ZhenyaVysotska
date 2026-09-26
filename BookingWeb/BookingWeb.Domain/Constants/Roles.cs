namespace BookingWeb.Domain;

public static class Roles
{
    public const string Admin = "Admin";
    public const string Host = "Host";
    public const string Client = "Client";
    
    public static readonly IReadOnlyList<string> All = [Admin, Host, Client];
}

