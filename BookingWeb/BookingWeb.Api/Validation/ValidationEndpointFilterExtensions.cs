using FluentValidation;

namespace BookingWeb.Api.Validation;

public sealed record ValidationEnableMetadata;

public static class ValidationEndpointFilterExtensions
{
    public static TBuilder WithValidation<TBuilder>(this TBuilder builder)
        where TBuilder : IEndpointConventionBuilder
    {
        builder.WithMetadata(new ValidationEnableMetadata());
        return builder.AddEndpointFilterFactory(CreateValidationFilter);
    }

    private static EndpointFilterDelegate CreateValidationFilter(
        EndpointFilterFactoryContext factoryContext,
        EndpointFilterDelegate next)
    {
        var validationTargets = FindValidationTargest(factoryContext);

        if (validationTargets.Count == 0)
        {
            return next;
        }

        return async invokationContext =>
        {
            foreach (var target in validationTargets)
            {
                var argument = invokationContext.Arguments[target.ArgumentIndex];
                if (argument is null)
                {
                    continue;
                }

                var validator = (IValidator)invokationContext.HttpContext.RequestServices
                    .GetRequiredService(target.ValidationType);

                var validationResult = await validator.ValidateAsync(
                    new ValidationContext<object>(argument),
                    invokationContext.HttpContext.RequestAborted);

                if (!validationResult.IsValid)
                {
                    return TypedResults.ValidationProblem(validationResult.ToDictionary());
                }
            }

            return await next(invokationContext);
        };
    }

    private static List<ValidationTarget> FindValidationTargest(EndpointFilterFactoryContext factoryContext)
    {
        var serviceChecker = factoryContext.ApplicationServices
            .GetRequiredService<IServiceProviderIsService>();

        var parameters = factoryContext.MethodInfo.GetParameters();
        var targets = new List<ValidationTarget>();

        for (var i = 0; i < parameters.Length; i++)
        {
            var validatorType = typeof(IValidator<>).MakeGenericType(parameters[i].ParameterType);

            if (serviceChecker.IsService(validatorType))
            {
                targets.Add(new ValidationTarget(i, validatorType));
            }
        }
        
        return targets;
    }
    
    private sealed record ValidationTarget(int ArgumentIndex, Type ValidationType);
}

