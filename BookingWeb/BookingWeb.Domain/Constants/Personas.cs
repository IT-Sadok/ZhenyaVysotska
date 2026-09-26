namespace BookingWeb.Domain;

public static class Personas
{
    public static readonly IReadOnlyList<string> Switchable = new[] { Roles.Client, Roles.Host };

    public static bool IsSwitchable(string persona) => Switchable.Contains(persona);
}
