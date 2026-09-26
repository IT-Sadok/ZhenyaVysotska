using BookingWeb.Application.Results;
using FluentValidation;
using FluentValidation.Results;

namespace BookingWeb.Application;

public static class ValidationExtensions
{
    public static async Task<ValidationError?> ValidateAllAsync<T>(
        this IEnumerable<IValidator<T>> validators, T instance, CancellationToken ct = default)
    {
        var failures = new List<ValidationFailure>();

        foreach (var validator in validators)
        {
            var result = await validator.ValidateAsync(instance, ct);
            if (!result.IsValid)
                failures.AddRange(result.Errors);
        }
        
        if(failures.Count == 0)
            return  null;

        var errors = failures
            .GroupBy(f => f.PropertyName)
            .ToDictionary(
                g => g.Key,
                g => g.Select(f => f.ErrorMessage).Distinct().ToArray());

        return new ValidationError(errors);
    } 
}

