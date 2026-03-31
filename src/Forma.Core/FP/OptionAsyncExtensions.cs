namespace Forma.Core.FP;

/// <summary>
/// Provides asynchronous extension methods for the Option class.
/// </summary>
public static class OptionAsyncExtensions
{
    /// <summary>
    /// Applies an asynchronous function to the value of the Option if it exists, returning a new Option.
    /// </summary>
    /// <typeparam name="T">The type of the current Option value.</typeparam>
    /// <typeparam name="TResult">The type of the result Option.</typeparam>
    /// <param name="optionTask">The task containing the Option.</param>
    /// <param name="binder">The asynchronous function to apply to the value.</param>
    /// <returns>A task containing a new Option with the result of the function, or None if the original Option was None.</returns>
    public static async Task<Option<TResult>> ThenAsync<T, TResult>(
        this Task<Option<T>> optionTask,
        Func<T, Task<Option<TResult>>> binder)
    {
        var option = await optionTask;
        return await option.ThenAsync(binder);
    }

    /// <summary>
    /// Executes an asynchronous action on the value of the Option if it exists, returning the same Option.
    /// </summary>
    /// <typeparam name="T">The type of the Option value.</typeparam>
    /// <param name="optionTask">The task containing the Option.</param>
    /// <param name="action">The asynchronous action to execute on the value.</param>
    /// <returns>A task containing the same Option instance.</returns>
    public static async Task<Option<T>> DoAsync<T>(
        this Task<Option<T>> optionTask,
        Func<T, Task> action)
    {
        var option = await optionTask;
        return await option.DoAsync(action);
    }

    /// <summary>
    /// Filters the Option based on an asynchronous predicate, returning None if the predicate fails.
    /// </summary>
    /// <typeparam name="T">The type of the Option value.</typeparam>
    /// <param name="optionTask">The task containing the Option.</param>
    /// <param name="predicate">The asynchronous predicate to test the value against.</param>
    /// <returns>A task containing the same Option if the predicate succeeds and the Option has a value, otherwise None.</returns>
    public static async Task<Option<T>> ValidateAsync<T>(
        this Task<Option<T>> optionTask,
        Func<T, Task<bool>> predicate)
    {
        var option = await optionTask;
        return await option.ValidateAsync(predicate);
    }
}
