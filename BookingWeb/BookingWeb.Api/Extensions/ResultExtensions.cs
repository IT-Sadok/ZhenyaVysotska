using BookingWeb.Application.Results;
namespace BookingWeb.Api.Extensions;

public static class ResultExtensions
{
    public static IResult ToProblem(this Result result)
    {
        if (result.IsSuccess)
            throw new InvalidOperationException("ToProblem was called on a successful result.");

        return result.Error switch
        {
            ValidationError v => ValidationProblem(v),
            { Code: "Auth.InvalidCredentials" } => Problem(result.Error, StatusCodes.Status401Unauthorized),
            { Code: var c } when c.EndsWith(".NotFound") => Problem(result.Error, StatusCodes.Status404NotFound),
            _ => Problem(result.Error, StatusCodes.Status400BadRequest)
        };
    }

    private static IResult ValidationProblem(ValidationError error)
    {
        var errors = error.Errors.ToDictionary(kv => kv.Key, kv => kv.Value);
        return Results.ValidationProblem(errors);
    }

    private static IResult Problem(Error error, int statusCode)
        => Results.Problem(detail: error.Description, statusCode: statusCode, title: error.Code);
}