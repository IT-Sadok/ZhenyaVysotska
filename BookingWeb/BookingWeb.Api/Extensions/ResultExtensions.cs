using BookingWeb.Application.Results;
namespace BookingWeb.Api.Extensions;

public static class ResultExtensions
{
    public static IResult Match<T>(this Result<T> result, Func<T, IResult> onSuccess)
    {
        if (result.IsFailure)
        {
            return result.ToProblem();
        }
        
        return onSuccess(result.Value);
    }

    private static IResult ToProblem(this Result result)
    {
        if (result.IsSuccess)
            throw new InvalidOperationException("ToProblem was called on a successful result.");

        return result.Error switch
        {
            { Type: ErrorType.NotFound } => Problem(result.Error, StatusCodes.Status404NotFound),
            { Type: ErrorType.Conflict } => Problem(result.Error, StatusCodes.Status409Conflict),
            { Type: ErrorType.Unauthorized } => Problem(result.Error, StatusCodes.Status401Unauthorized),
            _ => Problem(result.Error, StatusCodes.Status400BadRequest)
        };
    }
    
    private static IResult Problem(Error error, int statusCode)
        => Results.Problem(detail: error.Description, statusCode: statusCode, title: error.Code);
}