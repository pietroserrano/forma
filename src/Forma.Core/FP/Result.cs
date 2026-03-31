namespace Forma.Core.FP;

/// <summary>
/// Represents the result of an operation, which can be either a success or a failure.
/// </summary>
/// <typeparam name="T">The type of the success value.</typeparam>
public class Result<T>
    where T : notnull
{
    /// <summary>
    /// Gets a value indicating whether the result is a success.
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    /// Gets a value indicating whether the result is a failure.
    /// </summary>
    public bool IsFailure => !IsSuccess;

    /// <summary>
    /// Gets the success value if the result is a success; otherwise, null.
    /// </summary>
    public T? Value { get; }

    /// <summary>
    /// Gets the failure error if the result is a failure; otherwise, null.
    /// </summary>
    public Error? Error { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="Result{T}"/> class with a success value.
    /// </summary>
    /// <param name="value">The success value.</param>
    protected Result(T value)
    {
        IsSuccess = true;
        Value = value;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Result{T}"/> class with a failure error.
    /// </summary>
    /// <param name="error">The failure error.</param>
    protected Result(Error error)
    {
        IsSuccess = false;
        Error = error;
    }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    /// <param name="value">The success value.</param>
    /// <returns>A result representing a successful operation.</returns>
    public static Result<T> Success(T value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return new(value);
    }

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    /// <param name="error">The failure error.</param>
    /// <returns>A result representing a failed operation.</returns>
    public static Result<T> Failure(Error error) => new(error);

    /// <summary>
    /// Binds the result to a new function, transforming the success value.
    /// </summary>
    /// <typeparam name="U">The type of the success value of the resulting operation.</typeparam>
    /// <param name="func">The function to bind to the success value.</param>
    /// <returns>A new result representing the outcome of the function.</returns>
    public Result<U> Then<U>(Func<T, Result<U>> func) where U : notnull
    {
        return IsSuccess ? func(Value!) : Result<U>.Failure(Error!);
    }

    /// <summary>  
    /// Executes an action on the success value if the result is successful.  
    /// </summary>  
    /// <param name="action">The action to execute on the success value.</param>  
    /// <returns>The original result.</returns>  
    public Result<T> Do(Action<T> action)
    {
        if (IsSuccess) action(Value!);
        return this;
    }

    /// <summary>
    /// Ensures that the result satisfies a given predicate, otherwise returns a failure.
    /// </summary>
    /// <param name="predicate">The predicate to test the success value.</param>
    /// <param name="errorFactory">The factory function to create an error if the predicate fails.</param>
    /// <returns>The original result if the predicate is satisfied; otherwise, a failed result.</returns>
    public Result<T> Validate(Func<T, bool> predicate, Func<Error> errorFactory)
    {
        if (!IsSuccess) return this;
        return predicate(Value!) ? this : Failure(errorFactory());
    }

    /// <summary>  
    /// Matches the result to a function based on success or failure.  
    /// </summary>  
    /// <typeparam name="TResult">The type of the result returned by the functions.</typeparam>  
    /// <param name="onSuccess">The function to execute if the result is a success.</param>  
    /// <param name="onFailure">The function to execute if the result is a failure.</param>  
    /// <returns>The result of the executed function.</returns>  
    public TResult Match<TResult>(Func<T, TResult> onSuccess, Func<Error, TResult> onFailure)
    {
        return IsSuccess ? onSuccess(Value!) : onFailure(Error!);
    }

    /// <summary>
    /// Executes an action if the result is successful.
    /// </summary>
    /// <param name="action">The action to execute on the success value.</param>
    /// <returns>The original result.</returns>
    public Result<T> OnSuccess(Action<T> action)
    {
        if (IsSuccess) action(Value!);
        return this;
    }

    /// <summary>
    /// Executes an action if the result is a failure.
    /// </summary>
    /// <param name="action">The action to execute on the failure error.</param>
    /// <returns>The original result.</returns>
    public Result<T> OnError(Action<Error> action)
    {
        if (!IsSuccess) action(Error!);
        return this;
    }
}
