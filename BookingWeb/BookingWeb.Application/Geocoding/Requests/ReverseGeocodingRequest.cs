namespace BookingWeb.Application.Geocoding.Requests;

public sealed record ReverseGeocodingRequest(double? Lat, double? Lng);
