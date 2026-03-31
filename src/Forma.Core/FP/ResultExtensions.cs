namespace Forma.Core.FP;

/// <summary>
/// Extension methods for Result type, providing additional functional operations.
/// </summary>
public static class ResultExtensions
{
    /// <summary>
    /// Attempts to execute a function that may throw an exception, wrapping the result in a Result type.
    /// </summary>
    /// <typeparam name="TSuccess">The type of the success value.</typeparam>
    /// <param name="func">The function to execute.</param>
    /// <param name="errorMapper">Optional function to map exceptions to custom errors.</param>
    /// <returns>A Result containing either the success value or an error.</returns>
    public static Result<TSuccess, Error> Try<TSuccess>(
        Func<TSuccess> func,
        Func<Exception, Error>? errorMapper = null)
        where TSuccess : notnull
    {
        try
        {
            return Result<TSuccess, Error>.Success(func());
        }
        catch (Exception ex)
        {
            var error = errorMapper?.Invoke(ex)
                ?? new GenericError(ex.Message).WithMetadata("ExceptionType", ex.GetType().Name);
            return Result<TSuccess, Error>.Failure(error);
        }
    }

    /// <summary>
    /// Attempts to execute an asynchronous function that may throw an exception, wrapping the result in a Result type.
    /// </summary>
    /// <typeparam name="TSuccess">The type of the success value.</typeparam>
    /// <param name="func">The asynchronous function to execute.</param>
    /// <param name="errorMapper">Optional function to map exceptions to custom errors.</param>
    /// <returns>A Task containing a Result with either the success value or an error.</returns>
    public static async Task<Result<TSuccess, Error>> TryAsync<TSuccess>(
        Func<Task<TSuccess>> func,
        Func<Exception, Error>? errorMapper = null)
        where TSuccess : notnull
    {
        try
        {
            return Result<TSuccess, Error>.Success(await func());
        }
        catch (Exception ex)
        {
            var error = errorMapper?.Invoke(ex)
                ?? new GenericError(ex.Message).WithMetadata("ExceptionType", ex.GetType().Name);
            return Result<TSuccess, Error>.Failure(error);
        }
    }

    /// <summary>
    /// Recovers from an error by providing an alternative value.
    /// </summary>
    /// <typeparam name="TSuccess">The type of the success value.</typeparam>
    /// <typeparam name="TFailure">The type of the failure value.</typeparam>
    /// <param name="result">The result to recover from.</param>
    /// <param name="recovery">Function to produce a recovery value from the error.</param>
    /// <returns>The original result if successful, or a new successful result with the recovery value.</returns>
    public static Result<TSuccess, TFailure> Recover<TSuccess, TFailure>(
        this Result<TSuccess, TFailure> result,
        Func<TFailure, TSuccess?> recovery)
        where TSuccess : notnull
        where TFailure : notnull
    {
        if (result.IsSuccess) return result;

        var recovered = recovery(result.Error!);
        return recovered is not null
            ? Result<TSuccess, TFailure>.Success(recovered)
            : result;
    }

    /// <summary>
    /// Provides a fallback value if the result is a failure.
    /// </summary>
    /// <typeparam name="TSuccess">The type of the success value.</typeparam>
    /// <typeparam name="TFailure">The type of the failure value.</typeparam>
    /// <param name="result">The result.</param>
    /// <param name="fallback">The fallback value.</param>
    /// <returns>The original result if successful, or a new successful result with the fallback value.</returns>
    public static Result<TSuccess, TFailure> OrElse<TSuccess, TFailure>(
        this Result<TSuccess, TFailure> result,
        TSuccess fallback)
        where TSuccess : notnull
        where TFailure : notnull
    {
        return result.IsSuccess
            ? result
            : Result<TSuccess, TFailure>.Success(fallback);
    }

    /// <summary>
    /// Chains an alternative operation if the result is a failure.
    /// </summary>
    /// <typeparam name="TSuccess">The type of the success value.</typeparam>
    /// <typeparam name="TFailure">The type of the failure value.</typeparam>
    /// <param name="result">The result.</param>
    /// <param name="alternative">Function that produces an alternative result.</param>
    /// <returns>The original result if successful, or the result of the alternative function.</returns>
    public static Result<TSuccess, TFailure> OrElseTry<TSuccess, TFailure>(
        this Result<TSuccess, TFailure> result,
        Func<Result<TSuccess, TFailure>> alternative)
        where TSuccess : notnull
        where TFailure : notnull
    {
        return result.IsSuccess ? result : alternative();
    }

    /// <summary>
    /// Executes side effects for both success and failure without modifying the result (tap pattern).
    /// </summary>
    /// <typeparam name="TSuccess">The type of the success value.</typeparam>
    /// <typeparam name="TFailure">The type of the failure value.</typeparam>
    /// <param name="result">The result.</param>
    /// <param name="onSuccess">Action to execute on success.</param>
    /// <param name="onError">Action to execute on error.</param>
    /// <returns>The original result unchanged.</returns>
    public static Result<TSuccess, TFailure> Tap<TSuccess, TFailure>(
        this Result<TSuccess, TFailure> result,
        Action<TSuccess>? onSuccess = null,
        Action<TFailure>? onError = null)
        where TSuccess : notnull
        where TFailure : notnull
    {
        if (result.IsSuccess && onSuccess is not null)
            onSuccess(result.Value!);
        else if (!result.IsSuccess && onError is not null)
            onError(result.Error!);

        return result;
    }

    /// <summary>
    /// Validates all predicates and accumulates validation errors instead of short-circuiting.
    /// </summary>
    /// <typeparam name="TSuccess">The type of the success value.</typeparam>
    /// <param name="result">The result to validate.</param>
    /// <param name="validators">Array of validation functions.</param>
    /// <returns>A result with the validated value or accumulated validation errors.</returns>
    public static Result<TSuccess, ValidationError> ValidateAll<TSuccess>(
        this Result<TSuccess, Error> result,
        params Func<TSuccess, Result<TSuccess, ValidationError>>[] validators)
        where TSuccess : notnull
    {
        if (!result.IsSuccess)
            return Result<TSuccess, ValidationError>.Failure(
                new ValidationError("Validation failed", new Dictionary<string, string[]>()));

        var errors = new Dictionary<string, string[]>();

        foreach (var validator in validators)
        {
            var validationResult = validator(result.Value!);
            if (!validationResult.IsSuccess)
            {
                foreach (var (field, messages) in validationResult.Error!.Errors)
                {
                    if (errors.ContainsKey(field))
                        errors[field] = errors[field].Concat(messages).ToArray();
                    else
                        errors[field] = messages;
                }
            }
        }

        return errors.Any()
            ? Result<TSuccess, ValidationError>.Failure(
                new ValidationError("Validation failed", errors))
            : Result<TSuccess, ValidationError>.Success(result.Value!);
    }

    /// <summary>
    /// Combines multiple results into a single result. If all are successful, returns a tuple of values.
    /// If any fail, returns the first error.
    /// </summary>
    public static Result<(T1, T2), TFailure> Combine<T1, T2, TFailure>(
        Result<T1, TFailure> result1,
        Result<T2, TFailure> result2)
        where T1 : notnull
        where T2 : notnull
        where TFailure : notnull
    {
        if (!result1.IsSuccess) return Result<(T1, T2), TFailure>.Failure(result1.Error!);
        if (!result2.IsSuccess) return Result<(T1, T2), TFailure>.Failure(result2.Error!);

        return Result<(T1, T2), TFailure>.Success((result1.Value!, result2.Value!));
    }

    /// <summary>
    /// Combines three results into a single result.
    /// </summary>
    public static Result<(T1, T2, T3), TFailure> Combine<T1, T2, T3, TFailure>(
        Result<T1, TFailure> result1,
        Result<T2, TFailure> result2,
        Result<T3, TFailure> result3)
        where T1 : notnull
        where T2 : notnull
        where T3 : notnull
        where TFailure : notnull
    {
        if (!result1.IsSuccess) return Result<(T1, T2, T3), TFailure>.Failure(result1.Error!);
        if (!result2.IsSuccess) return Result<(T1, T2, T3), TFailure>.Failure(result2.Error!);
        if (!result3.IsSuccess) return Result<(T1, T2, T3), TFailure>.Failure(result3.Error!);

        return Result<(T1, T2, T3), TFailure>.Success((result1.Value!, result2.Value!, result3.Value!));
    }
}
