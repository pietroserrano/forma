using Forma.Core.FP;

namespace Forma.Tests.FP;

public class ResultTests
{
    #region Success Tests

    [Fact]
    public void Success_CreatesSuccessResult()
    {
        // Arrange & Act
        var result = Result<int>.Success(42);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(42, result.Value);
        Assert.Null(result.Error);
    }

    [Fact]
    public void Failure_CreatesFailureResult()
    {
        // Arrange & Act
        var result = Result<int>.Failure(Error.Generic("Error occurred"));

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Error occurred", result.Error.Message);
    }

    #endregion

    #region Then Tests

    [Fact]
    public void Then_OnSuccess_TransformsValue()
    {
        // Arrange
        var result = Result<int>.Success(5);

        // Act
        var transformed = result.Then(x => Result<int>.Success(x * 2));

        // Assert
        Assert.True(transformed.IsSuccess);
        Assert.Equal(10, transformed.Value);
    }

    [Fact]
    public void Then_OnFailure_PropagatesError()
    {
        // Arrange
        var result = Result<int>.Failure(Error.Generic("Initial error"));

        // Act
        var transformed = result.Then(x => Result<int>.Success(x * 2));

        // Assert
        Assert.False(transformed.IsSuccess);
        Assert.Equal("Initial error", transformed.Error.Message);
    }

    [Fact]
    public void Then_CanChainMultipleOperations()
    {
        // Arrange
        var result = Result<int>.Success(5);

        // Act
        var transformed = result
            .Then(x => Result<int>.Success(x * 2))
            .Then(x => Result<string>.Success($"Value: {x}"));

        // Assert
        Assert.True(transformed.IsSuccess);
        Assert.Equal("Value: 10", transformed.Value);
    }

    #endregion

    #region Do Tests

    [Fact]
    public void Do_OnSuccess_ExecutesAction()
    {
        // Arrange
        var result = Result<int>.Success(42);
        var executed = false;

        // Act
        var returned = result.Do(_ => executed = true);

        // Assert
        Assert.True(executed);
        Assert.Same(result, returned);
    }

    [Fact]
    public void Do_OnFailure_DoesNotExecuteAction()
    {
        // Arrange
        var result = Result<int>.Failure(Error.Generic("Error"));
        var executed = false;

        // Act
        var returned = result.Do(_ => executed = true);

        // Assert
        Assert.False(executed);
        Assert.Same(result, returned);
    }

    #endregion

    #region Validate Tests

    [Fact]
    public void Validate_OnSuccess_WithValidPredicate_ReturnsOriginalResult()
    {
        // Arrange
        var result = Result<int>.Success(10);

        // Act
        var validated = result.Validate(x => x > 5, () => Error.Generic("Value too small"));

        // Assert
        Assert.True(validated.IsSuccess);
        Assert.Equal(10, validated.Value);
    }

    [Fact]
    public void Validate_OnSuccess_WithInvalidPredicate_ReturnsFailure()
    {
        // Arrange
        var result = Result<int>.Success(3);

        // Act
        var validated = result.Validate(x => x > 5, () => Error.Generic("Value too small"));

        // Assert
        Assert.False(validated.IsSuccess);
        Assert.Equal("Value too small", validated.Error.Message);
    }

    [Fact]
    public void Validate_OnFailure_ReturnsOriginalFailure()
    {
        // Arrange
        var result = Result<int>.Failure(Error.Generic("Original error"));

        // Act
        var validated = result.Validate(x => x > 5, () => Error.Generic("Value too small"));

        // Assert
        Assert.False(validated.IsSuccess);
        Assert.Equal("Original error", validated.Error.Message);
    }

    #endregion

    #region Match Tests

    [Fact]
    public void Match_OnSuccess_ExecutesSuccessFunction()
    {
        // Arrange
        var result = Result<int>.Success(42);

        // Act
        var matched = result.Match(
            onSuccess: x => $"Success: {x}",
            onFailure: e => $"Failure: {e.Message}"
        );

        // Assert
        Assert.Equal("Success: 42", matched);
    }

    [Fact]
    public void Match_OnFailure_ExecutesFailureFunction()
    {
        // Arrange
        var result = Result<int>.Failure(Error.Generic("Error occurred"));

        // Act
        var matched = result.Match(
            onSuccess: x => $"Success: {x}",
            onFailure: e => $"Failure: {e.Message}"
        );

        // Assert
        Assert.Equal("Failure: Error occurred", matched);
    }

    #endregion

    #region OnSuccess and OnError Tests

    [Fact]
    public void OnSuccess_OnSuccess_ExecutesAction()
    {
        // Arrange
        var result = Result<int>.Success(42);
        var executedWithValue = 0;

        // Act
        var returned = result.OnSuccess(x => executedWithValue = x);

        // Assert
        Assert.Equal(42, executedWithValue);
        Assert.Same(result, returned);
    }

    [Fact]
    public void OnSuccess_OnFailure_DoesNotExecuteAction()
    {
        // Arrange
        var result = Result<int>.Failure(Error.Generic("Error"));
        var executed = false;

        // Act
        var returned = result.OnSuccess(_ => executed = true);

        // Assert
        Assert.False(executed);
        Assert.Same(result, returned);
    }

    [Fact]
    public void OnError_OnFailure_ExecutesAction()
    {
        // Arrange
        var result = Result<int>.Failure(Error.Generic("Error occurred"));
        var executedError = "";

        // Act
        var returned = result.OnError(e => executedError = e.Message);

        // Assert
        Assert.Equal("Error occurred", executedError);
        Assert.Same(result, returned);
    }

    [Fact]
    public void OnError_OnSuccess_DoesNotExecuteAction()
    {
        // Arrange
        var result = Result<int>.Success(42);
        var executed = false;

        // Act
        var returned = result.OnError(_ => executed = true);

        // Assert
        Assert.False(executed);
        Assert.Same(result, returned);
    }

    #endregion

    #region Complex Pipeline Tests

    [Fact]
    public void ComplexPipeline_AllSuccessful_ReturnsSuccessResult()
    {
        // Arrange
        var parseResult = ParseInt("42");

        // Act
        var pipeline = parseResult
            .Then(x => Result<int>.Success(x * 2))
            .Then(x => Result<string>.Success($"Result: {x}"))
            .Do(s => Console.WriteLine(s));

        // Assert
        Assert.True(pipeline.IsSuccess);
        Assert.Equal("Result: 84", pipeline.Value);
    }

    [Fact]
    public void ComplexPipeline_WithFailure_ShortCircuits()
    {
        // Arrange
        var parseResult = ParseInt("invalid");
        var step2Called = false;

        // Act
        var pipeline = parseResult
            .Then(x =>
            {
                step2Called = true;
                return Result<int>.Success(x * 2);
            });

        // Assert
        Assert.False(pipeline.IsSuccess);
        Assert.False(step2Called);
        Assert.Equal("Invalid number format", pipeline.Error.Message);
    }

    [Fact]
    public void ComplexPipeline_WithValidation_FailsOnInvalidData()
    {
        // Arrange
        var result = Result<int>.Success(3);

        // Act
        var pipeline = result
            .Validate(x => x > 5, () => Error.Generic("Number must be greater than 5"))
            .Then(x => Result<string>.Success($"Valid: {x}"));

        // Assert
        Assert.False(pipeline.IsSuccess);
        Assert.Equal("Number must be greater than 5", pipeline.Error.Message);
    }

    #endregion

    #region Helper Methods

    private static Result<int> ParseInt(string s)
    {
        if (int.TryParse(s, out int val))
            return Result<int>.Success(val);
        return Result<int>.Failure(Error.Generic("Invalid number format"));
    }

    #endregion
}
