namespace BookingWeb.Domain;

public static class Roles
{
    public const string Admin = "Admin";
    
    public const string Host = "Host";
    
    public const string Client = "Client";
    
    public static readonly IReadOnlyList<string> All = [Admin, Host, Client];
    
    public static readonly IReadOnlyList<string> SelfAssignable = [Client, Host];

    public static string? GetExactRoleName(string? roleName)
    {
        return All.FirstOrDefault(r => string.Equals(r, roleName, StringComparison.OrdinalIgnoreCase));
    }
}

