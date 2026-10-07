using BookingWeb.Application.Results;
using Shouldly;

namespace BookingWeb.UnitTests.Results;

public sealed class ResultTests
{
    private static readonly Error SomeError = new("Test.Error", "Something went wrong.");
    
    private static Result<int> ReturnValue(int value) => value;
    private static Result<int> ReturnError(Error error) => error;

    [Fact]
    public void ImplicitConversion_ShouldCreateSuccess_WhenValueIsReturned()
    {
        var result = ReturnValue(42);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(42);
        result.Error.ShouldBe(Error.None);
    }

    [Fact]
    public void ImplicitConversion_ShouldCreateFailure_WhenErrorIsReturned()
    {
        var result = ReturnError(SomeError);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(SomeError);
    }

    [Fact]
    public void Value_ShouldThrow_WhenResultIsFailure()
    {
        var result = ReturnError(SomeError);

        Should.Throw<InvalidOperationException>(() => result.Value);
    }

    [Fact]
    public void Failure_ShouldThrow_WhenErrorIsNone()
    {
        Should.Throw<ArgumentException>(() => Result.Failure<int>(Error.None));
    }
    
    [Fact]
    public void Error_ShouldHaveFailureType_WhenTypeIsNotSpecified()
    {
        var error = new Error("Test.Code", "Description");

        error.Type.ShouldBe(ErrorType.Failure);
    }
}
