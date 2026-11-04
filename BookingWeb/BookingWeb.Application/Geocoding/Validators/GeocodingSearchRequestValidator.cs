using BookingWeb.Application.Geocoding.Requests;
using FluentValidation;

namespace BookingWeb.Application.Geocoding.Validators;

public sealed class GeocodingSearchRequestValidator : AbstractValidator<GeocodingSearchRequest>
{
    public const int MinQueryLength = 3;
    public const int MaxQueryLength = 200;

    public GeocodingSearchRequestValidator()
    {
        RuleFor(x => x.Q).NotEmpty();
        RuleFor(x => x.Q)
            .Must(q => q!.Trim().Length is >= MinQueryLength and <= MaxQueryLength)
            .When(x => !string.IsNullOrWhiteSpace(x.Q))
            .WithMessage($"Query must be between {MinQueryLength} and {MaxQueryLength} characters.");
    }
}
