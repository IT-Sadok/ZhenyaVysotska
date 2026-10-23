using System.Runtime.CompilerServices;

namespace BookingWeb.Domain;

internal static class Guard
{
    public static void ThrowIfEmpty(
        Guid argument,
        [CallerArgumentExpression(nameof(argument))] string? paramName = null)
    {
        if (argument == Guid.Empty)
        {
            throw new ArgumentException("Value cannot be an empty GUID.", paramName);
        }
    }
}