using Forma.Core.FP;

namespace Forma.Tests.FP;

public class ResultExtensionsTests
{
    #region Try Tests

    [Fact]
    public void Try_WhenFuncSucceeds_ReturnsSuccess()
    {
        // Act
        var result = ResultExtensions.Try(() => 42);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(42, result.Value);
    }

    [Fact]
    public void Try_WhenFuncThrows_ReturnsFailure()
    {
        // Act
        var result = ResultExtensions.Try<int>(() => throw new InvalidOperationException("boom"));

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("boom", result.Error!.Message);
    }

    [Fact]
    public void Try_WithCustomErrorMapper_UsesMapper()
    {
        // Act
        var result = ResultExtensions.Try<int>(
            () => throw new InvalidOperationException("boom"),
            ex => Error.Generic($"Mapped: {ex.Message}"));

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Mapped: boom", result.Error!.Message);
    }

    [Fact]
    public void Try_WithNullFunc_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => ResultExtensions.Try<int>(null!));
    }

    #endregion

    #region TryAsync Tests

    [Fact]
    public async Task TryAsync_WhenFuncSucceeds_ReturnsSuccess()
    {
        // Act
        var result = await ResultExtensions.TryAsync(async () =>
        {
            await Task.Yield();
            return 42;
        });

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(42, result.Value);
    }

    [Fact]
    public async Task TryAsync_WhenFuncThrows_ReturnsFailure()
    {
        // Act
        var result = await ResultExtensions.TryAsync<int>(async () =>
        {
            await Task.Yield();
            throw new InvalidOperationException("async boom");
        });

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("async boom", result.Error!.Message);
    }

    [Fact]
    public async Task TryAsync_WithCustomErrorMapper_UsesMapper()
    {
        // Act
        var result = await ResultExtensions.TryAsync<int>(
            async () =>
            {
                await Task.Yield();
                throw new InvalidOperationException("async boom");
            },
            ex => Error.Generic($"Mapped: {ex.Message}"));

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Mapped: async boom", result.Error!.Message);
    }

    [Fact]
    public async Task TryAsync_WithNullFunc_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => ResultExtensions.TryAsync<int>(null!));
    }

    #endregion

    #region Recover Tests

    [Fact]
    public void Recover_OnSuccess_ReturnsOriginal()
    {
        // Arrange
        var result = Result<int>.Success(42);

        // Act
        var recovered = result.Recover(_ => 99);

        // Assert
        Assert.True(recovered.IsSuccess);
        Assert.Equal(42, recovered.Value);
    }

    [Fact]
    public void Recover_OnFailure_WithRecoveryValue_ReturnsSuccess()
    {
        // Arrange
        var result = Result<int>.Failure(Error.Generic("error"));

        // Act
        var recovered = result.Recover(_ => 99);

        // Assert
        Assert.True(recovered.IsSuccess);
        Assert.Equal(99, recovered.Value);
    }

    [Fact]
    public void Recover_OnFailure_WhenRecoveryReturnsNull_ReturnsOriginalFailure()
    {
        // Arrange
        var result = Result<string>.Failure(Error.Generic("error"));

        // Act
        var recovered = result.Recover(_ => (string?)null);

        // Assert
        Assert.False(recovered.IsSuccess);
        Assert.Equal("error", recovered.Error!.Message);
    }

    [Fact]
    public void Recover_WithNullRecovery_ThrowsArgumentNullException()
    {
        // Arrange
        var result = Result<int>.Failure(Error.Generic("error"));

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            ResultExtensions.Recover<int>(result, null!));
    }

    #endregion

    #region OrElse Tests

    [Fact]
    public void OrElse_OnSuccess_ReturnsOriginal()
    {
        // Arrange
        var result = Result<int>.Success(42);

        // Act
        var orElse = result.OrElse(99);

        // Assert
        Assert.True(orElse.IsSuccess);
        Assert.Equal(42, orElse.Value);
    }

    [Fact]
    public void OrElse_OnFailure_ReturnsFallback()
    {
        // Arrange
        var result = Result<int>.Failure(Error.Generic("error"));

        // Act
        var orElse = result.OrElse(99);

        // Assert
        Assert.True(orElse.IsSuccess);
        Assert.Equal(99, orElse.Value);
    }

    #endregion

    #region OrElseTry Tests

    [Fact]
    public void OrElseTry_OnSuccess_ReturnsOriginal()
    {
        // Arrange
        var result = Result<int>.Success(42);
        var alternativeCalled = false;

        // Act
        var orElse = result.OrElseTry(() =>
        {
            alternativeCalled = true;
            return Result<int>.Success(99);
        });

        // Assert
        Assert.True(orElse.IsSuccess);
        Assert.Equal(42, orElse.Value);
        Assert.False(alternativeCalled);
    }

    [Fact]
    public void OrElseTry_OnFailure_InvokesAlternative()
    {
        // Arrange
        var result = Result<int>.Failure(Error.Generic("error"));

        // Act
        var orElse = result.OrElseTry(() => Result<int>.Success(99));

        // Assert
        Assert.True(orElse.IsSuccess);
        Assert.Equal(99, orElse.Value);
    }

    [Fact]
    public void OrElseTry_WithNullAlternative_ThrowsArgumentNullException()
    {
        // Arrange
        var result = Result<int>.Failure(Error.Generic("error"));

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            result.OrElseTry((Func<Result<int>>)null!));
    }

    #endregion

    #region Tap Tests

    [Fact]
    public void Tap_OnSuccess_InvokesOnSuccessAction()
    {
        // Arrange
        var result = Result<int>.Success(42);
        var captured = 0;

        // Act
        var tapped = result.Tap(onSuccess: v => captured = v);

        // Assert
        Assert.True(tapped.IsSuccess);
        Assert.Equal(42, captured);
    }

    [Fact]
    public void Tap_OnFailure_InvokesOnErrorAction()
    {
        // Arrange
        var error = Error.Generic("test error");
        var result = Result<int>.Failure(error);
        Error? captured = null;

        // Act
        var tapped = result.Tap(onError: e => captured = e);

        // Assert
        Assert.False(tapped.IsSuccess);
        Assert.Equal("test error", captured!.Message);
    }

    [Fact]
    public void Tap_ReturnsOriginalResultUnchanged()
    {
        // Arrange
        var result = Result<int>.Success(42);

        // Act
        var tapped = result.Tap(onSuccess: _ => { });

        // Assert
        Assert.Same(result, tapped);
    }

    #endregion

    #region Helper Methods

    private static ValidationError FieldError(string field, params string[] messages) =>
        new("Validation", new Dictionary<string, string[]> { [field] = messages });

    #endregion

    #region ValidateAll Tests

    [Fact]
    public void ValidateAll_AllValid_ReturnsSuccess()
    {
        // Arrange
        var result = Result<int>.Success(10);

        // Act
        var validated = result.ValidateAll(
            x => x > 0
                ? Result<int>.Success(x)
                : Result<int>.Failure(FieldError("value", "Must be positive")),
            x => x < 100
                ? Result<int>.Success(x)
                : Result<int>.Failure(FieldError("value", "Must be less than 100"))
        );

        // Assert
        Assert.True(validated.IsSuccess);
        Assert.Equal(10, validated.Value);
    }

    [Fact]
    public void ValidateAll_MultipleValidationErrors_AccumulatesAll()
    {
        // Arrange
        var result = Result<string>.Success("x");

        // Act
        var validated = result.ValidateAll(
            _ => Result<string>.Failure(FieldError("name", "Too short")),
            _ => Result<string>.Failure(FieldError("name", "Invalid characters"))
        );

        // Assert
        Assert.False(validated.IsSuccess);
        var validationError = Assert.IsType<ValidationError>(validated.Error);
        Assert.Contains("name", validationError.Errors.Keys);
        Assert.Equal(2, validationError.Errors["name"].Length);
    }

    [Fact]
    public void ValidateAll_NonValidationError_FailsFast()
    {
        // Arrange
        var result = Result<int>.Success(10);
        var secondValidatorCalled = false;

        // Act
        var validated = result.ValidateAll(
            _ => Result<int>.Failure(Error.Generic("Non-validation error")),
            x =>
            {
                secondValidatorCalled = true;
                return Result<int>.Success(x);
            }
        );

        // Assert
        Assert.False(validated.IsSuccess);
        Assert.Equal("Non-validation error", validated.Error!.Message);
        Assert.False(secondValidatorCalled);
    }

    [Fact]
    public void ValidateAll_OnFailureInput_ReturnsInputFailureImmediately()
    {
        // Arrange
        var result = Result<int>.Failure(Error.Generic("original error"));
        var validatorCalled = false;

        // Act
        var validated = result.ValidateAll(
            x =>
            {
                validatorCalled = true;
                return Result<int>.Success(x);
            }
        );

        // Assert
        Assert.False(validated.IsSuccess);
        Assert.Equal("original error", validated.Error!.Message);
        Assert.False(validatorCalled);
    }

    #endregion

    #region Combine Tests

    [Fact]
    public void Combine_TwoSuccesses_ReturnsTuple()
    {
        // Arrange
        var r1 = Result<int>.Success(1);
        var r2 = Result<string>.Success("hello");

        // Act
        var combined = ResultExtensions.Combine(r1, r2);

        // Assert
        Assert.True(combined.IsSuccess);
        Assert.Equal((1, "hello"), combined.Value);
    }

    [Fact]
    public void Combine_FirstFailure_ReturnsFirstError()
    {
        // Arrange
        var r1 = Result<int>.Failure(Error.Generic("first error"));
        var r2 = Result<string>.Success("hello");

        // Act
        var combined = ResultExtensions.Combine(r1, r2);

        // Assert
        Assert.False(combined.IsSuccess);
        Assert.Equal("first error", combined.Error!.Message);
    }

    [Fact]
    public void Combine_SecondFailure_ReturnsSecondError()
    {
        // Arrange
        var r1 = Result<int>.Success(1);
        var r2 = Result<string>.Failure(Error.Generic("second error"));

        // Act
        var combined = ResultExtensions.Combine(r1, r2);

        // Assert
        Assert.False(combined.IsSuccess);
        Assert.Equal("second error", combined.Error!.Message);
    }

    [Fact]
    public void Combine_ThreeSuccesses_ReturnsTuple()
    {
        // Arrange
        var r1 = Result<int>.Success(1);
        var r2 = Result<string>.Success("hello");
        var r3 = Result<bool>.Success(true);

        // Act
        var combined = ResultExtensions.Combine(r1, r2, r3);

        // Assert
        Assert.True(combined.IsSuccess);
        Assert.Equal((1, "hello", true), combined.Value);
    }

    [Fact]
    public void Combine_ThirdOfThreeFailure_ReturnsThirdError()
    {
        // Arrange
        var r1 = Result<int>.Success(1);
        var r2 = Result<string>.Success("hello");
        var r3 = Result<bool>.Failure(Error.Generic("third error"));

        // Act
        var combined = ResultExtensions.Combine(r1, r2, r3);

        // Assert
        Assert.False(combined.IsSuccess);
        Assert.Equal("third error", combined.Error!.Message);
    }

    #endregion

    #region IsFailure Tests

    [Fact]
    public void IsFailure_OnSuccess_ReturnsFalse()
    {
        // Arrange
        var result = Result<int>.Success(42);

        // Assert
        Assert.False(result.IsFailure);
    }

    [Fact]
    public void IsFailure_OnFailure_ReturnsTrue()
    {
        // Arrange
        var result = Result<int>.Failure(Error.Generic("error"));

        // Assert
        Assert.True(result.IsFailure);
    }

    #endregion

    #region Success Null Validation Tests

    [Fact]
    public void Success_WithNull_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => Result<string>.Success(null!));
    }

    #endregion

    #region ValidationError Defensive Copy Tests

    [Fact]
    public void ValidationError_MutatingSourceDictionary_DoesNotAffectError()
    {
        // Arrange
        var dict = new Dictionary<string, string[]> { ["name"] = ["Too short"] };
        var error = new ValidationError("Validation", dict);

        // Act – mutate the original dictionary
        dict["name"] = ["Mutated"];
        dict["email"] = ["New field"];

        // Assert – error is unchanged
        Assert.Single(error.Errors);
        Assert.Contains("name", error.Errors.Keys);
        Assert.Equal("Too short", error.Errors["name"][0]);
    }

    [Fact]
    public void ValidationError_MutatingSourceArray_DoesNotAffectError()
    {
        // Arrange
        var messages = new[] { "Too short" };
        var dict = new Dictionary<string, string[]> { ["name"] = messages };
        var error = new ValidationError("Validation", dict);

        // Act – mutate the original array
        messages[0] = "Mutated";

        // Assert – error is unchanged
        Assert.Equal("Too short", error.Errors["name"][0]);
    }

    [Fact]
    public void ValidationError_WithNullErrors_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new ValidationError("msg", (IReadOnlyDictionary<string, string[]>)null!));
    }

    #endregion

    #region AggregateError Defensive Copy Tests

    [Fact]
    public void AggregateError_MutatingSourceList_DoesNotAffectError()
    {
        // Arrange
        var list = new List<Error> { Error.Generic("first") };
        var error = new AggregateError("Multiple errors", list);

        // Act – mutate the original list
        list.Add(Error.Generic("second"));

        // Assert – error is unchanged
        Assert.Single(error.InnerErrors);
        Assert.Equal("first", error.InnerErrors[0].Message);
    }

    [Fact]
    public void AggregateError_WithNullInnerErrors_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new AggregateError("msg", (IReadOnlyList<Error>)null!));
    }

    #endregion

    #region ValidationError Combine Array Clone Tests

    [Fact]
    public void ValidationErrorCombine_MutatingSourceArrayAfterCombine_DoesNotAffectCombinedError()
    {
        // Arrange
        var messages1 = new[] { "Too short" };
        var first = new ValidationError("first", new Dictionary<string, string[]> { ["name"] = messages1 });
        var second = new ValidationError("second", new Dictionary<string, string[]> { ["email"] = new[] { "Invalid" } });

        // Act
        var combined = first.Combine(second);

        // Mutate the original source array
        messages1[0] = "Mutated";

        // Assert – combined error is unaffected
        Assert.Equal("Too short", combined.Errors["name"][0]);
    }

    [Fact]
    public void ValidationErrorCombine_SharedKey_MergesWithoutSharingArrayReference()
    {
        // Arrange
        var first = new ValidationError("first", new Dictionary<string, string[]> { ["name"] = new[] { "Too short" } });
        var second = new ValidationError("second", new Dictionary<string, string[]> { ["name"] = new[] { "Invalid chars" } });

        // Act
        var combined = first.Combine(second);

        // Assert – both messages accumulated
        Assert.Equal(2, combined.Errors["name"].Length);
        Assert.Contains("Too short", combined.Errors["name"]);
        Assert.Contains("Invalid chars", combined.Errors["name"]);
    }

    #endregion
}
