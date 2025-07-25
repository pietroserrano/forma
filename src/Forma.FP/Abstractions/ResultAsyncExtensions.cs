namespace Forma.FP.Abstractions;

/// <summary>
/// Provides asynchronous extension methods for the Result class.
/// </summary>
public static class ResultAsyncExtensions
{
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TSuccess"></typeparam>
    /// <typeparam name="TError"></typeparam>
    /// <typeparam name="TNextSuccess"></typeparam>
    /// <param name="resultTask"></param>
    /// <param name="next"></param>
    /// <returns></returns>
    public static async Task<Result<TNextSuccess, TError>> ThenAsync<TSuccess, TError, TNextSuccess>(
        this Task<Result<TSuccess, TError>> resultTask,
        Func<TSuccess, Task<Result<TNextSuccess, TError>>> next) 
        where TSuccess : notnull
        where TNextSuccess : notnull
        where TError : notnull
    {
        var result = await resultTask;
        return await result.ThenAsync(next);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TSuccess"></typeparam>
    /// <typeparam name="TError"></typeparam>
    /// <typeparam name="TNextSuccess"></typeparam>
    /// <param name="result"></param>
    /// <param name="next"></param>
    /// <returns></returns>
    public static async Task<Result<TNextSuccess, TError>> ThenAsync<TSuccess, TError, TNextSuccess>(
        this Result<TSuccess, TError> result,
        Func<TSuccess, Task<Result<TNextSuccess, TError>>> next)
        where TSuccess : notnull
        where TNextSuccess : notnull
        where TError : notnull
    {
        if (!result.IsSuccess)
            return Result<TNextSuccess, TError>.Failure(result.Error!);

        return await next(result.Value!);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TSuccess"></typeparam>
    /// <typeparam name="TError"></typeparam>
    /// <param name="resultTask"></param>
    /// <param name="action"></param>
    /// <returns></returns>
    public static async Task<Result<TSuccess, TError>> DoAsync<TSuccess, TError>(
        this Task<Result<TSuccess, TError>> resultTask,
        Func<TSuccess, Task> action) where TSuccess : notnull
        where TError : notnull
    {
        var result = await resultTask;
        if (result.IsSuccess)
            await action(result.Value!);
        return result;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TSuccess"></typeparam>
    /// <typeparam name="TError"></typeparam>
    /// <param name="resultTask"></param>
    /// <param name="predicate"></param>
    /// <param name="errorFactory"></param>
    /// <returns></returns>
    public static async Task<Result<TSuccess, TError>> ValidateAsync<TSuccess, TError>(
        this Task<Result<TSuccess, TError>> resultTask,
        Func<TSuccess, Task<bool>> predicate,
        Func<TError> errorFactory) where TSuccess : notnull
        where TError : notnull
    {
        var result = await resultTask;
        if (!result.IsSuccess)
            return result;

        return await predicate(result.Value!)
            ? result
            : Result<TSuccess, TError>.Failure(errorFactory());
    }

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TSuccess"></typeparam>
    /// <typeparam name="TError"></typeparam>
    /// <typeparam name="TResult"></typeparam>
    /// <param name="resultTask"></param>
    /// <param name="onSuccess"></param>
    /// <param name="onFailure"></param>
    /// <returns></returns>
    public static async Task<TResult> MatchAsync<TSuccess, TError, TResult>(
        this Task<Result<TSuccess, TError>> resultTask,
        Func<TSuccess, Task<TResult>> onSuccess,
        Func<TError, Task<TResult>> onFailure) where TSuccess : notnull
        where TError : notnull
    {
        var result = await resultTask;
        return result.IsSuccess
            ? await onSuccess(result.Value!)
            : await onFailure(result.Error!);
    }
}
