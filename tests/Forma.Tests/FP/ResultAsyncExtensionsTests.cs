using Forma.Core.FP;

namespace Forma.Tests.FP;

public class ResultAsyncExtensionsTests
{
    #region ThenAsync Tests

    [Fact]
    public async Task ThenAsync_OnSuccessTask_TransformsValue()
    {
        // Arrange
        var resultTask = Task.FromResult(Result<int, string>.Success(5));

        // Act
        var transformed = await resultTask.ThenAsync(async x =>
        {
            await Task.Delay(1);
            return Result<int, string>.Success(x * 2);
        });

        // Assert
        Assert.True(transformed.IsSuccess);
        Assert.Equal(10, transformed.Value);
    }

    [Fact]
    public async Task ThenAsync_OnFailureTask_PropagatesError()
    {
        // Arrange
        var resultTask = Task.FromResult(Result<int, string>.Failure("Initial error"));

        // Act
        var transformed = await resultTask.ThenAsync(async x =>
        {
            await Task.Delay(1);
            return Result<int, string>.Success(x * 2);
        });

        // Assert
        Assert.False(transformed.IsSuccess);
        Assert.Equal("Initial error", transformed.Error);
    }

    [Fact]
    public async Task ThenAsync_OnSuccess_CanChainMultiple()
    {
        // Arrange
        var resultTask = Task.FromResult(Result<int, string>.Success(5));

        // Act
        var transformed = await resultTask
            .ThenAsync(async x =>
            {
                await Task.Delay(1);
                return Result<int, string>.Success(x * 2);
            })
            .ThenAsync(async x =>
            {
                await Task.Delay(1);
                return Result<string, string>.Success($"Value: {x}");
            });

        // Assert
        Assert.True(transformed.IsSuccess);
        Assert.Equal("Value: 10", transformed.Value);
    }

    [Fact]
    public async Task ThenAsync_WithResultInstance_TransformsValue()
    {
        // Arrange
        var result = Result<int, string>.Success(5);

        // Act
        var transformed = await result.ThenAsync(async x =>
        {
            await Task.Delay(1);
            return Result<int, string>.Success(x * 2);
        });

        // Assert
        Assert.True(transformed.IsSuccess);
        Assert.Equal(10, transformed.Value);
    }

    #endregion

    #region DoAsync Tests

    [Fact]
    public async Task DoAsync_OnSuccess_ExecutesAction()
    {
        // Arrange
        var resultTask = Task.FromResult(Result<int, string>.Success(42));
        var executed = false;
        var executedValue = 0;

        // Act
        var returned = await resultTask.DoAsync(async x =>
        {
            await Task.Delay(1);
            executed = true;
            executedValue = x;
        });

        // Assert
        Assert.True(executed);
        Assert.Equal(42, executedValue);
        Assert.True(returned.IsSuccess);
        Assert.Equal(42, returned.Value);
    }

    [Fact]
    public async Task DoAsync_OnFailure_DoesNotExecuteAction()
    {
        // Arrange
        var resultTask = Task.FromResult(Result<int, string>.Failure("Error"));
        var executed = false;

        // Act
        var returned = await resultTask.DoAsync(async _ =>
        {
            await Task.Delay(1);
            executed = true;
        });

        // Assert
        Assert.False(executed);
        Assert.False(returned.IsSuccess);
        Assert.Equal("Error", returned.Error);
    }

    #endregion

    #region ValidateAsync Tests

    [Fact]
    public async Task ValidateAsync_OnSuccess_WithValidPredicate_ReturnsOriginalResult()
    {
        // Arrange
        var resultTask = Task.FromResult(Result<int, string>.Success(10));

        // Act
        var validated = await resultTask.ValidateAsync(
            async x =>
            {
                await Task.Delay(1);
                return x > 5;
            },
            () => "Value too small"
        );

        // Assert
        Assert.True(validated.IsSuccess);
        Assert.Equal(10, validated.Value);
    }

    [Fact]
    public async Task ValidateAsync_OnSuccess_WithInvalidPredicate_ReturnsFailure()
    {
        // Arrange
        var resultTask = Task.FromResult(Result<int, string>.Success(3));

        // Act
        var validated = await resultTask.ValidateAsync(
            async x =>
            {
                await Task.Delay(1);
                return x > 5;
            },
            () => "Value too small"
        );

        // Assert
        Assert.False(validated.IsSuccess);
        Assert.Equal("Value too small", validated.Error);
    }

    [Fact]
    public async Task ValidateAsync_OnFailure_ReturnsOriginalFailure()
    {
        // Arrange
        var resultTask = Task.FromResult(Result<int, string>.Failure("Original error"));

        // Act
        var validated = await resultTask.ValidateAsync(
            async x =>
            {
                await Task.Delay(1);
                return x > 5;
            },
            () => "Value too small"
        );

        // Assert
        Assert.False(validated.IsSuccess);
        Assert.Equal("Original error", validated.Error);
    }

    #endregion

    #region MatchAsync Tests

    [Fact]
    public async Task MatchAsync_OnSuccess_ExecutesSuccessFunction()
    {
        // Arrange
        var resultTask = Task.FromResult(Result<int, string>.Success(42));

        // Act
        var matched = await resultTask.MatchAsync(
            onSuccess: async x =>
            {
                await Task.Delay(1);
                return $"Success: {x}";
            },
            onFailure: async e =>
            {
                await Task.Delay(1);
                return $"Failure: {e}";
            }
        );

        // Assert
        Assert.Equal("Success: 42", matched);
    }

    [Fact]
    public async Task MatchAsync_OnFailure_ExecutesFailureFunction()
    {
        // Arrange
        var resultTask = Task.FromResult(Result<int, string>.Failure("Error occurred"));

        // Act
        var matched = await resultTask.MatchAsync(
            onSuccess: async x =>
            {
                await Task.Delay(1);
                return $"Success: {x}";
            },
            onFailure: async e =>
            {
                await Task.Delay(1);
                return $"Failure: {e}";
            }
        );

        // Assert
        Assert.Equal("Failure: Error occurred", matched);
    }

    #endregion

    #region Complex Pipeline Tests

    [Fact]
    public async Task ComplexAsyncPipeline_AllSuccessful_ReturnsSuccessResult()
    {
        // Arrange
        var resultTask = ParseIntAsync("42");

        // Act
        var pipeline = await resultTask
            .ThenAsync(async x =>
            {
                await Task.Delay(1);
                return Result<int, string>.Success(x * 2);
            })
            .DoAsync(async x =>
            {
                await Task.Delay(1);
                Console.WriteLine($"Processing: {x}");
            })
            .ValidateAsync(
                async x =>
                {
                    await Task.Delay(1);
                    return x > 50;
                },
                () => "Value must be greater than 50"
            )
            .ThenAsync(async x =>
            {
                await Task.Delay(1);
                return Result<string, string>.Success($"Result: {x}");
            });

        // Assert
        Assert.True(pipeline.IsSuccess);
        Assert.Equal("Result: 84", pipeline.Value);
    }

    [Fact]
    public async Task ComplexAsyncPipeline_WithFailure_ShortCircuits()
    {
        // Arrange
        var resultTask = ParseIntAsync("invalid");
        var step2Called = false;

        // Act
        var pipeline = await resultTask
            .ThenAsync(async x =>
            {
                await Task.Delay(1);
                step2Called = true;
                return Result<int, string>.Success(x * 2);
            });

        // Assert
        Assert.False(pipeline.IsSuccess);
        Assert.False(step2Called);
        Assert.Equal("Invalid number format", pipeline.Error);
    }

    [Fact]
    public async Task ComplexAsyncPipeline_FailsValidation_ReturnsFailure()
    {
        // Arrange
        var resultTask = Task.FromResult(Result<int, string>.Success(30));

        // Act
        var pipeline = await resultTask
            .ValidateAsync(
                async x =>
                {
                    await Task.Delay(1);
                    return x > 50;
                },
                () => "Value must be greater than 50"
            )
            .ThenAsync(async x =>
            {
                await Task.Delay(1);
                return Result<string, string>.Success($"Valid: {x}");
            });

        // Assert
        Assert.False(pipeline.IsSuccess);
        Assert.Equal("Value must be greater than 50", pipeline.Error);
    }

    [Fact]
    public async Task ComplexAsyncPipeline_WithMatchAsync_ReturnsCorrectString()
    {
        // Arrange
        var resultTask = ParseIntAsync("42");

        // Act
        var result = await resultTask
            .ThenAsync(async x =>
            {
                await Task.Delay(1);
                return Result<int, string>.Success(x * 2);
            })
            .MatchAsync(
                onSuccess: async x =>
                {
                    await Task.Delay(1);
                    return $"Computed: {x}";
                },
                onFailure: async e =>
                {
                    await Task.Delay(1);
                    return $"Error: {e}";
                }
            );

        // Assert
        Assert.Equal("Computed: 84", result);
    }

    #endregion

    #region Helper Methods

    private static Task<Result<int, string>> ParseIntAsync(string s)
    {
        if (int.TryParse(s, out int val))
            return Task.FromResult(Result<int, string>.Success(val));
        return Task.FromResult(Result<int, string>.Failure("Invalid number format"));
    }

    #endregion
}
