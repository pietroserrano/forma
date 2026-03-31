using Forma.Core.FP;

namespace Forma.Tests.FP;

public class OptionTests
{
    #region Creation Tests

    [Fact]
    public void Some_CreatesOptionWithValue()
    {
        // Arrange & Act
        var option = Option<int>.Some(42);

        // Assert
        Assert.True(option.IsSome);
        Assert.False(option.IsNone);
    }

    [Fact]
    public void None_CreatesOptionWithoutValue()
    {
        // Arrange & Act
        var option = Option<int>.None();

        // Assert
        Assert.False(option.IsSome);
        Assert.True(option.IsNone);
    }

    [Fact]
    public void From_WithNonNullValue_CreatesSome()
    {
        // Arrange
        string? value = "test";

        // Act
        var option = Option<string>.From(value);

        // Assert
        Assert.True(option.IsSome);
    }

    [Fact]
    public void From_WithNullValue_CreatesNone()
    {
        // Arrange
        string? value = null;

        // Act
        var option = Option<string>.From(value);

        // Assert
        Assert.True(option.IsNone);
    }

    #endregion

    #region Then Tests

    [Fact]
    public void Then_OnSome_TransformsValue()
    {
        // Arrange
        var option = Option<int>.Some(5);

        // Act
        var transformed = option.Then(x => Option<int>.Some(x * 2));

        // Assert
        Assert.True(transformed.IsSome);
    }

    [Fact]
    public void Then_OnNone_ReturnsNone()
    {
        // Arrange
        var option = Option<int>.None();

        // Act
        var transformed = option.Then(x => Option<int>.Some(x * 2));

        // Assert
        Assert.True(transformed.IsNone);
    }

    [Fact]
    public void Then_CanChainMultipleOperations()
    {
        // Arrange
        var option = Option<int>.Some(5);

        // Act
        var transformed = option
            .Then(x => Option<int>.Some(x * 2))
            .Then(x => Option<string>.Some($"Value: {x}"));

        // Assert
        Assert.True(transformed.IsSome);
    }

    [Fact]
    public void Then_ChainBreaksOnNone()
    {
        // Arrange
        var option = Option<int>.Some(5);
        var step3Called = false;

        // Act
        var transformed = option
            .Then(x => Option<int>.Some(x * 2))
            .Then(_ => Option<int>.None())
            .Then(x =>
            {
                step3Called = true;
                return Option<string>.Some($"Value: {x}");
            });

        // Assert
        Assert.True(transformed.IsNone);
        Assert.False(step3Called);
    }

    #endregion

    #region ThenAsync Tests

    [Fact]
    public async Task ThenAsync_OnSome_TransformsValue()
    {
        // Arrange
        var option = Option<int>.Some(5);

        // Act
        var transformed = await option.ThenAsync(async x =>
        {
            await Task.Delay(1);
            return Option<int>.Some(x * 2);
        });

        // Assert
        Assert.True(transformed.IsSome);
    }

    [Fact]
    public async Task ThenAsync_OnNone_ReturnsNone()
    {
        // Arrange
        var option = Option<int>.None();

        // Act
        var transformed = await option.ThenAsync(async x =>
        {
            await Task.Delay(1);
            return Option<int>.Some(x * 2);
        });

        // Assert
        Assert.True(transformed.IsNone);
    }

    #endregion

    #region Do Tests

    [Fact]
    public void Do_OnSome_ExecutesAction()
    {
        // Arrange
        var option = Option<int>.Some(42);
        var executed = false;

        // Act
        var returned = option.Do(_ => executed = true);

        // Assert
        Assert.True(executed);
        Assert.Same(option, returned);
    }

    [Fact]
    public void Do_OnNone_DoesNotExecuteAction()
    {
        // Arrange
        var option = Option<int>.None();
        var executed = false;

        // Act
        var returned = option.Do(_ => executed = true);

        // Assert
        Assert.False(executed);
        Assert.Same(option, returned);
    }

    #endregion

    #region DoAsync Tests

    [Fact]
    public async Task DoAsync_OnSome_ExecutesAction()
    {
        // Arrange
        var option = Option<int>.Some(42);
        var executed = false;

        // Act
        var returned = await option.DoAsync(async _ =>
        {
            await Task.Delay(1);
            executed = true;
        });

        // Assert
        Assert.True(executed);
        Assert.Same(option, returned);
    }

    [Fact]
    public async Task DoAsync_OnNone_DoesNotExecuteAction()
    {
        // Arrange
        var option = Option<int>.None();
        var executed = false;

        // Act
        var returned = await option.DoAsync(async _ =>
        {
            await Task.Delay(1);
            executed = true;
        });

        // Assert
        Assert.False(executed);
        Assert.Same(option, returned);
    }

    #endregion

    #region Validate Tests

    [Fact]
    public void Validate_OnSome_WithValidPredicate_ReturnsSame()
    {
        // Arrange
        var option = Option<int>.Some(10);

        // Act
        var validated = option.Validate(x => x > 5);

        // Assert
        Assert.True(validated.IsSome);
    }

    [Fact]
    public void Validate_OnSome_WithInvalidPredicate_ReturnsNone()
    {
        // Arrange
        var option = Option<int>.Some(3);

        // Act
        var validated = option.Validate(x => x > 5);

        // Assert
        Assert.True(validated.IsNone);
    }

    [Fact]
    public void Validate_OnNone_ReturnsNone()
    {
        // Arrange
        var option = Option<int>.None();

        // Act
        var validated = option.Validate(x => x > 5);

        // Assert
        Assert.True(validated.IsNone);
    }

    #endregion

    #region ValidateAsync Tests

    [Fact]
    public async Task ValidateAsync_OnSome_WithValidPredicate_ReturnsSame()
    {
        // Arrange
        var option = Option<int>.Some(10);

        // Act
        var validated = await option.ValidateAsync(async x =>
        {
            await Task.Delay(1);
            return x > 5;
        });

        // Assert
        Assert.True(validated.IsSome);
    }

    [Fact]
    public async Task ValidateAsync_OnSome_WithInvalidPredicate_ReturnsNone()
    {
        // Arrange
        var option = Option<int>.Some(3);

        // Act
        var validated = await option.ValidateAsync(async x =>
        {
            await Task.Delay(1);
            return x > 5;
        });

        // Assert
        Assert.True(validated.IsNone);
    }

    [Fact]
    public async Task ValidateAsync_OnNone_ReturnsNone()
    {
        // Arrange
        var option = Option<int>.None();

        // Act
        var validated = await option.ValidateAsync(async x =>
        {
            await Task.Delay(1);
            return x > 5;
        });

        // Assert
        Assert.True(validated.IsNone);
    }

    #endregion

    #region Match Tests

    [Fact]
    public void Match_OnSome_ExecutesSomeFunction()
    {
        // Arrange
        var option = Option<int>.Some(42);

        // Act
        var result = option.Match(
            some: x => $"Value: {x}",
            none: () => "No value"
        );

        // Assert
        Assert.Equal("Value: 42", result);
    }

    [Fact]
    public void Match_OnNone_ExecutesNoneFunction()
    {
        // Arrange
        var option = Option<int>.None();

        // Act
        var result = option.Match(
            some: x => $"Value: {x}",
            none: () => "No value"
        );

        // Assert
        Assert.Equal("No value", result);
    }

    #endregion

    #region Complex Pipeline Tests

    [Fact]
    public void ComplexPipeline_AllSome_ReturnsTransformedValue()
    {
        // Arrange
        var option = ParseInt("42");

        // Act
        var result = option
            .Then(x => Option<int>.Some(x * 2))
            .Validate(x => x > 50)
            .Then(x => Option<string>.Some($"Result: {x}"))
            .Match(
                some: s => s,
                none: () => "Failed"
            );

        // Assert
        Assert.Equal("Result: 84", result);
    }

    [Fact]
    public void ComplexPipeline_WithNone_ShortCircuits()
    {
        // Arrange
        var option = ParseInt("invalid");
        var step2Called = false;

        // Act
        var result = option
            .Then(x =>
            {
                step2Called = true;
                return Option<int>.Some(x * 2);
            })
            .Match(
                some: x => $"Value: {x}",
                none: () => "No value"
            );

        // Assert
        Assert.Equal("No value", result);
        Assert.False(step2Called);
    }

    [Fact]
    public void ComplexPipeline_FailsValidation_ReturnsNone()
    {
        // Arrange
        var option = Option<int>.Some(3);

        // Act
        var result = option
            .Validate(x => x > 5)
            .Then(x => Option<string>.Some($"Valid: {x}"))
            .Match(
                some: s => s,
                none: () => "Validation failed"
            );

        // Assert
        Assert.Equal("Validation failed", result);
    }

    [Fact]
    public async Task ComplexAsyncPipeline_AllSuccessful_ReturnsValue()
    {
        // Arrange
        var option = Option<int>.Some(5);

        // Act
        var result = await option
            .DoAsync(async x =>
            {
                await Task.Delay(1);
                Console.WriteLine($"Processing: {x}");
            })
            .ThenAsync(async x =>
            {
                await Task.Delay(1);
                return Option<int>.Some(x * 2);
            })
            .ValidateAsync(async x =>
            {
                await Task.Delay(1);
                return x > 5;
            });

        // Assert
        Assert.True(result.IsSome);
    }

    #endregion

    #region Helper Methods

    private static Option<int> ParseInt(string s)
    {
        if (int.TryParse(s, out int val))
            return Option<int>.Some(val);
        return Option<int>.None();
    }

    #endregion
}
