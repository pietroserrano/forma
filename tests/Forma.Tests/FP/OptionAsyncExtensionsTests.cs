using Forma.Core.FP;

namespace Forma.Tests.FP;

public class OptionAsyncExtensionsTests
{
    #region ThenAsync Tests

    [Fact]
    public async Task ThenAsync_OnSomeTask_AppliesBinder()
    {
        // Arrange
        var optionTask = Task.FromResult(Option<int>.Some(5));

        // Act
        var result = await optionTask.ThenAsync(async x =>
        {
            await Task.Yield();
            return Option<int>.Some(x * 2);
        });

        // Assert
        Assert.True(result.IsSome);
        var value = result.Match(some: v => v, none: () => 0);
        Assert.Equal(10, value);
    }

    [Fact]
    public async Task ThenAsync_OnNoneTask_DoesNotInvokeBinder()
    {
        // Arrange
        var optionTask = Task.FromResult(Option<int>.None());
        var binderCalled = false;

        // Act
        var result = await optionTask.ThenAsync(async x =>
        {
            binderCalled = true;
            await Task.Yield();
            return Option<int>.Some(x * 2);
        });

        // Assert
        Assert.False(result.IsSome);
        Assert.False(binderCalled);
    }

    [Fact]
    public async Task ThenAsync_OnSome_BinderCanReturnNone()
    {
        // Arrange
        var optionTask = Task.FromResult(Option<int>.Some(5));

        // Act
        var result = await optionTask.ThenAsync(async _ =>
        {
            await Task.Yield();
            return Option<int>.None();
        });

        // Assert
        Assert.False(result.IsSome);
    }

    [Fact]
    public async Task ThenAsync_OnSomeTask_CanChain()
    {
        // Arrange
        var optionTask = Task.FromResult(Option<int>.Some(3));

        // Act
        var result = await optionTask
            .ThenAsync(async x =>
            {
                await Task.Yield();
                return Option<int>.Some(x * 2);
            })
            .ThenAsync(async x =>
            {
                await Task.Yield();
                return Option<string>.Some($"Value: {x}");
            });

        // Assert
        Assert.True(result.IsSome);
        var value = result.Match(some: v => v, none: () => string.Empty);
        Assert.Equal("Value: 6", value);
    }

    [Fact]
    public async Task ThenAsync_OnNoneTask_PropagatesNoneThroughChain()
    {
        // Arrange
        var optionTask = Task.FromResult(Option<int>.None());
        var secondBinderCalled = false;

        // Act
        var result = await optionTask
            .ThenAsync(async x =>
            {
                await Task.Yield();
                return Option<int>.Some(x * 2);
            })
            .ThenAsync(async x =>
            {
                secondBinderCalled = true;
                await Task.Yield();
                return Option<string>.Some($"Value: {x}");
            });

        // Assert
        Assert.False(result.IsSome);
        Assert.False(secondBinderCalled);
    }

    #endregion

    #region DoAsync Tests

    [Fact]
    public async Task DoAsync_OnSomeTask_ExecutesAction()
    {
        // Arrange
        var optionTask = Task.FromResult(Option<int>.Some(42));
        var captured = 0;

        // Act
        var result = await optionTask.DoAsync(async x =>
        {
            await Task.Yield();
            captured = x;
        });

        // Assert
        Assert.True(result.IsSome);
        Assert.Equal(42, captured);
    }

    [Fact]
    public async Task DoAsync_OnNoneTask_DoesNotExecuteAction()
    {
        // Arrange
        var optionTask = Task.FromResult(Option<int>.None());
        var actionCalled = false;

        // Act
        var result = await optionTask.DoAsync(async _ =>
        {
            actionCalled = true;
            await Task.Yield();
        });

        // Assert
        Assert.False(result.IsSome);
        Assert.False(actionCalled);
    }

    [Fact]
    public async Task DoAsync_ReturnsOriginalOption()
    {
        // Arrange
        var option = Option<int>.Some(42);
        var optionTask = Task.FromResult(option);

        // Act
        var result = await optionTask.DoAsync(async _ => await Task.Yield());

        // Assert
        Assert.True(result.IsSome);
        var value = result.Match(some: v => v, none: () => 0);
        Assert.Equal(42, value);
    }

    #endregion

    #region ValidateAsync Tests

    [Fact]
    public async Task ValidateAsync_OnSomeTask_WhenPredicateTrue_ReturnsSome()
    {
        // Arrange
        var optionTask = Task.FromResult(Option<int>.Some(10));

        // Act
        var result = await optionTask.ValidateAsync(async x =>
        {
            await Task.Yield();
            return x > 5;
        });

        // Assert
        Assert.True(result.IsSome);
        var value = result.Match(some: v => v, none: () => 0);
        Assert.Equal(10, value);
    }

    [Fact]
    public async Task ValidateAsync_OnSomeTask_WhenPredicateFalse_ReturnsNone()
    {
        // Arrange
        var optionTask = Task.FromResult(Option<int>.Some(3));

        // Act
        var result = await optionTask.ValidateAsync(async x =>
        {
            await Task.Yield();
            return x > 5;
        });

        // Assert
        Assert.False(result.IsSome);
    }

    [Fact]
    public async Task ValidateAsync_OnNoneTask_DoesNotInvokePredicate()
    {
        // Arrange
        var optionTask = Task.FromResult(Option<int>.None());
        var predicateCalled = false;

        // Act
        var result = await optionTask.ValidateAsync(async _ =>
        {
            predicateCalled = true;
            await Task.Yield();
            return true;
        });

        // Assert
        Assert.False(result.IsSome);
        Assert.False(predicateCalled);
    }

    #endregion

    #region Null Guard Tests

    [Fact]
    public async Task ThenAsync_WithNullTask_ThrowsArgumentNullException()
    {
        // Arrange
        Task<Option<int>> nullTask = null!;

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            nullTask.ThenAsync(x => Task.FromResult(Option<int>.Some(x))));
    }

    [Fact]
    public async Task ThenAsync_WithNullBinder_ThrowsArgumentNullException()
    {
        // Arrange
        var optionTask = Task.FromResult(Option<int>.Some(5));

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            optionTask.ThenAsync((Func<int, Task<Option<int>>>)null!));
    }

    [Fact]
    public async Task DoAsync_WithNullTask_ThrowsArgumentNullException()
    {
        // Arrange
        Task<Option<int>> nullTask = null!;

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            nullTask.DoAsync(_ => Task.CompletedTask));
    }

    [Fact]
    public async Task DoAsync_WithNullAction_ThrowsArgumentNullException()
    {
        // Arrange
        var optionTask = Task.FromResult(Option<int>.Some(5));

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            optionTask.DoAsync((Func<int, Task>)null!));
    }

    [Fact]
    public async Task ValidateAsync_WithNullTask_ThrowsArgumentNullException()
    {
        // Arrange
        Task<Option<int>> nullTask = null!;

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            nullTask.ValidateAsync(_ => Task.FromResult(true)));
    }

    [Fact]
    public async Task ValidateAsync_WithNullPredicate_ThrowsArgumentNullException()
    {
        // Arrange
        var optionTask = Task.FromResult(Option<int>.Some(5));

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            optionTask.ValidateAsync((Func<int, Task<bool>>)null!));
    }

    #endregion
}
