namespace BookingWeb.Application.Results;

public enum ErrorType
{
    Failure,
    NotFound,
    Conflict,
    Unauthorized
}
public record Error(string Code, string Description,  ErrorType Type = ErrorType.Failure)
{
    public static readonly Error None = new(string.Empty, string.Empty);
}

