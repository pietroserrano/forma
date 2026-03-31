namespace Forma.Core.FP;

/// <summary>
/// Extension methods for Result type, providing additional functional operations.
/// </summary>
public static class ResultExtensions
{
    /// <summary>
    /// Attempts to execute a function that may throw an exception, wrapping the result in a Result type.
    /// </summary>
    /// <typeparam name="T">The type of the success value.</typeparam>
    /// <param name="func">The function to execute.</param>
    /// <param name="errorMapper">Optional function to map exceptions to custom errors.</param>
    /// <returns>A Result containing either the success value or an error.</returns>
    public static Result<T> Try<T>(
        Func<T> func,
        Func<Exception, Error>? errorMapper = null)
        where T : notnull
    {
        try
        {
            return Result<T>.Success(func());
        }
        catch (Exception ex)
        {
            var error = errorMapper?.Invoke(ex)
                ?? new GenericError(ex.Message).WithMetadata("ExceptionType", ex.GetType().Name);
            return Result<T>.Failure(error);
        }
    }

    /// <summary>
    /// Attempts to execute an asynchronous function that may throw an exception, wrapping the result in a Result type.
    /// </summary>
    /// <typeparam name="T">The type of the success value.</typeparam>
    /// <param name="func">The asynchronous function to execute.</param>
    /// <param name="errorMapper">Optional function to map exceptions to custom errors.</param>
    /// <returns>A Task containing a Result with either the success value or an error.</returns>
    public static async Task<Result<T>> TryAsync<T>(
        Func<Task<T>> func,
        Func<Exception, Error>? errorMapper = null)
        where T : notnull
    {
        try
        {
            return Result<T>.Success(await func());
        }
        catch (Exception ex)
        {
            var error = errorMapper?.Invoke(ex)
                ?? new GenericError(ex.Message).WithMetadata("ExceptionType", ex.GetType().Name);
            return Result<T>.Failure(error);
        }
    }

    /// <summary>
    /// Recovers from an error by providing an alternative value.
    /// </summary>
    /// <typeparam name="T">The type of the success value.</typeparam>
    /// <param name="result">The result to recover from.</param>
    /// <param name="recovery">Function to produce a recovery value from the error.</param>
    /// <returns>The original result if successful, or a new successful result with the recovery value.</returns>
    public static Result<T> Recover<T>(
        this Result<T> result,
        Func<Error, T?> recovery)
        where T : notnull
    {
        if (result.IsSuccess) return result;

        var recovered = recovery(result.Error!);
        return recovered is not null
            ? Result<T>.Success(recovered)
            : result;
    }

    /// <summary>
    /// Provides a fallback value if the result is a failure.
    /// </summary>
    /// <typeparam name="T">The type of the success value.</typeparam>
    /// <param name="result">The result.</param>
    /// <param name="fallback">The fallback value.</param>
    /// <returns>The original result if successful, or a new successful result with the fallback value.</returns>
    public static Result<T> OrElse<T>(
        this Result<T> result,
        T fallback)
        where T : notnull
    {
        return result.IsSuccess
            ? result
            : Result<T>.Success(fallback);
    }

    /// <summary>
    /// Chains an alternative operation if the result is a failure.
    /// </summary>
    /// <typeparam name="T">The type of the success value.</typeparam>
    /// <param name="result">The result.</param>
    /// <param name="alternative">Function that produces an alternative result.</param>
    /// <returns>The original result if successful, or the result of the alternative function.</returns>
    public static Result<T> OrElseTry<T>(
        this Result<T> result,
        Func<Result<T>> alternative)
        where T : notnull
    {
        return result.IsSuccess ? result : alternative();
    }

    /// <summary>
    /// Executes side effects for both success and failure without modifying the result (tap pattern).
    /// </summary>
    /// <typeparam name="T">The type of the success value.</typeparam>
    /// <param name="result">The result.</param>
    /// <param name="onSuccess">Action to execute on success.</param>
    /// <param name="onError">Action to execute on error.</param>
    /// <returns>The original result unchanged.</returns>
    public static Result<T> Tap<T>(
        this Result<T> result,
        Action<T>? onSuccess = null,
        Action<Error>? onError = null)
        where T : notnull
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
    /// <typeparam name="T">The type of the success value.</typeparam>
    /// <param name="result">The result to validate.</param>
    /// <param name="validators">Array of validation functions.</param>
    /// <returns>A result with the validated value or accumulated validation errors.</returns>
    public static Result<T> ValidateAll<T>(
        this Result<T> result,
        params Func<T, Result<T>>[] validators)
        where T : notnull
    {
        if (!result.IsSuccess)
            return result;

        var errors = new Dictionary<string, string[]>();

        foreach (var validator in validators)
        {
            var validationResult = validator(result.Value!);
            if (!validationResult.IsSuccess)
            {
                // If it's a ValidationError, accumulate the errors
                if (validationResult.Error is ValidationError validationError)
                {
                    foreach (var (field, messages) in validationError.Errors)
                    {
                        if (errors.ContainsKey(field))
                            errors[field] = errors[field].Concat(messages).ToArray();
                        else
                            errors[field] = messages;
                    }
                }
            }
        }

        return errors.Any()
            ? Result<T>.Failure(new ValidationError("Validation failed", errors))
            : Result<T>.Success(result.Value!);
    }

    /// <summary>
    /// Combines multiple results into a single result. If all are successful, returns a tuple of values.
    /// If any fail, returns the first error.
    /// </summary>
    public static Result<(T1, T2)> Combine<T1, T2>(
        Result<T1> result1,
        Result<T2> result2)
        where T1 : notnull
        where T2 : notnull
    {
        if (!result1.IsSuccess) return Result<(T1, T2)>.Failure(result1.Error!);
        if (!result2.IsSuccess) return Result<(T1, T2)>.Failure(result2.Error!);

        return Result<(T1, T2)>.Success((result1.Value!, result2.Value!));
    }

    /// <summary>
    /// Combines three results into a single result.
    /// </summary>
    public static Result<(T1, T2, T3)> Combine<T1, T2, T3>(
        Result<T1> result1,
        Result<T2> result2,
        Result<T3> result3)
        where T1 : notnull
        where T2 : notnull
        where T3 : notnull
    {
        if (!result1.IsSuccess) return Result<(T1, T2, T3)>.Failure(result1.Error!);
        if (!result2.IsSuccess) return Result<(T1, T2, T3)>.Failure(result2.Error!);
        if (!result3.IsSuccess) return Result<(T1, T2, T3)>.Failure(result3.Error!);

        return Result<(T1, T2, T3)>.Success((result1.Value!, result2.Value!, result3.Value!));
    }
}
