namespace Forma.Core.FP;

/// <summary>
/// Provides asynchronous extension methods for the Result class.
/// </summary>
public static class ResultAsyncExtensions
{
    /// <summary>
    /// Asynchronously chains a <see cref="Task{TResult}"/> of <see cref="Result{T}"/> to a next async operation,
    /// propagating the failure without invoking <paramref name="next"/> if the result is unsuccessful.
    /// </summary>
    /// <typeparam name="T">The type of the success value of the current result.</typeparam>
    /// <typeparam name="TNext">The type of the success value of the next result.</typeparam>
    /// <param name="resultTask">A task that produces the current result.</param>
    /// <param name="next">An asynchronous function to invoke with the success value if the current result is successful.</param>
    /// <returns>A task representing the result of the next operation, or the original failure if the current result is unsuccessful.</returns>
    public static async Task<Result<TNext>> ThenAsync<T, TNext>(
        this Task<Result<T>> resultTask,
        Func<T, Task<Result<TNext>>> next) 
        where T : notnull
        where TNext : notnull
    {
        var result = await resultTask;
        return await result.ThenAsync(next);
    }

    /// <summary>
    /// Asynchronously chains a <see cref="Result{T}"/> to a next async operation,
    /// propagating the failure without invoking <paramref name="next"/> if the result is unsuccessful.
    /// </summary>
    /// <typeparam name="T">The type of the success value of the current result.</typeparam>
    /// <typeparam name="TNext">The type of the success value of the next result.</typeparam>
    /// <param name="result">The current result.</param>
    /// <param name="next">An asynchronous function to invoke with the success value if the current result is successful.</param>
    /// <returns>A task representing the result of the next operation, or the original failure if the current result is unsuccessful.</returns>
    public static async Task<Result<TNext>> ThenAsync<T, TNext>(
        this Result<T> result,
        Func<T, Task<Result<TNext>>> next)
        where T : notnull
        where TNext : notnull
    {
        ArgumentNullException.ThrowIfNull(next);
        if (!result.IsSuccess)
            return Result<TNext>.Failure(result.Error!);

        return await next(result.Value!);
    }

    /// <summary>
    /// Asynchronously executes a side-effect action on the success value of a result task without altering the result.
    /// The action is only invoked when the result is successful.
    /// </summary>
    /// <typeparam name="T">The type of the success value.</typeparam>
    /// <param name="resultTask">A task that produces the current result.</param>
    /// <param name="action">An asynchronous action to invoke with the success value if the result is successful.</param>
    /// <returns>A task representing the original result, unchanged.</returns>
    public static async Task<Result<T>> DoAsync<T>(
        this Task<Result<T>> resultTask,
        Func<T, Task> action) 
        where T : notnull
    {
        ArgumentNullException.ThrowIfNull(resultTask);
        ArgumentNullException.ThrowIfNull(action);
        var result = await resultTask;
        if (result.IsSuccess)
            await action(result.Value!);
        return result;
    }

    /// <summary>
    /// Asynchronously validates the success value of a result task against a predicate,
    /// returning a failure produced by <paramref name="errorFactory"/> if the predicate is not satisfied.
    /// The validation is skipped if the result is already a failure.
    /// </summary>
    /// <typeparam name="T">The type of the success value.</typeparam>
    /// <param name="resultTask">A task that produces the current result.</param>
    /// <param name="predicate">An asynchronous predicate to test the success value.</param>
    /// <param name="errorFactory">A factory function that produces the error when the predicate fails.</param>
    /// <returns>A task representing the original result if validation passes, or a failed result if it does not.</returns>
    public static async Task<Result<T>> ValidateAsync<T>(
        this Task<Result<T>> resultTask,
        Func<T, Task<bool>> predicate,
        Func<Error> errorFactory) 
        where T : notnull
    {
        ArgumentNullException.ThrowIfNull(resultTask);
        ArgumentNullException.ThrowIfNull(predicate);
        ArgumentNullException.ThrowIfNull(errorFactory);
        var result = await resultTask;
        if (!result.IsSuccess)
            return result;

        return await predicate(result.Value!)
            ? result
            : Result<T>.Failure(errorFactory());
    }

    /// <summary>
    /// Asynchronously matches the outcome of a result task by invoking either <paramref name="onSuccess"/>
    /// or <paramref name="onFailure"/> depending on whether the result is successful.
    /// </summary>
    /// <typeparam name="T">The type of the success value.</typeparam>
    /// <typeparam name="TResult">The type of the value returned by both handler functions.</typeparam>
    /// <param name="resultTask">A task that produces the current result.</param>
    /// <param name="onSuccess">An asynchronous function to invoke with the success value when the result is successful.</param>
    /// <param name="onFailure">An asynchronous function to invoke with the error value when the result is a failure.</param>
    /// <returns>A task representing the value returned by whichever handler was invoked.</returns>
    public static async Task<TResult> MatchAsync<T, TResult>(
        this Task<Result<T>> resultTask,
        Func<T, Task<TResult>> onSuccess,
        Func<Error, Task<TResult>> onFailure) 
        where T : notnull
    {
        ArgumentNullException.ThrowIfNull(resultTask);
        ArgumentNullException.ThrowIfNull(onSuccess);
        ArgumentNullException.ThrowIfNull(onFailure);
        var result = await resultTask;
        return result.IsSuccess
            ? await onSuccess(result.Value!)
            : await onFailure(result.Error!);
    }
}
