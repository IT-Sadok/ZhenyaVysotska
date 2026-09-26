namespace BookingWeb.Application.Results;

public sealed record ValidationError(IReadOnlyDictionary<string, string[]> Errors)
    : Error("Validation.Failed", "One or more parameters are invalid");