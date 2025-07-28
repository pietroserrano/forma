using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Forma.Core.FP;

/// <summary>
/// Represents the result of an operation, which can be either a success or a failure.
/// </summary>
/// <typeparam name="TSuccess">The type of the success value.</typeparam>
/// <typeparam name="TFailure">The type of the failure value.</typeparam>
public class Result<TSuccess, TFailure>
    where TSuccess : notnull
    where TFailure : notnull
{
    /// <summary>
    /// Gets a value indicating whether the result is a success.
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    /// Gets the success value if the result is a success; otherwise, null.
    /// </summary>
    public TSuccess? Value { get; }

    /// <summary>
    /// Gets the failure error if the result is a failure; otherwise, null.
    /// </summary>
    public TFailure? Error { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="Result{TSuccess, TFailure}"/> class with a success value.
    /// </summary>
    /// <param name="value">The success value.</param>
    protected Result(TSuccess value)
    {
        IsSuccess = true;
        Value = value;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Result{TSuccess, TFailure}"/> class with a failure error.
    /// </summary>
    /// <param name="error">The failure error.</param>
    protected Result(TFailure error)
    {
        IsSuccess = false;
        Error = error;
    }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    /// <param name="value">The success value.</param>
    /// <returns>A result representing a successful operation.</returns>
    public static Result<TSuccess, TFailure> Success(TSuccess value) => new(value);

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    /// <param name="error">The failure error.</param>
    /// <returns>A result representing a failed operation.</returns>
    public static Result<TSuccess, TFailure> Failure(TFailure error) => new(error);

    /// <summary>
    /// Binds the result to a new function, transforming the success value.
    /// </summary>
    /// <typeparam name="U">The type of the success value of the resulting operation.</typeparam>
    /// <param name="func">The function to bind to the success value.</param>
    /// <returns>A new result representing the outcome of the function.</returns>
    public Result<U, TFailure> Then<U>(Func<TSuccess, Result<U, TFailure>> func) where U : notnull
    {
        return IsSuccess ? func(Value!) : Result<U, TFailure>.Failure(Error!);
    }

    /// <summary>  
    /// Executes an action on the success value if the result is successful.  
    /// </summary>  
    /// <param name="action">The action to execute on the success value.</param>  
    /// <returns>The original result.</returns>  
    public Result<TSuccess, TFailure> Do(Action<TSuccess> action)
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
    public Result<TSuccess, TFailure> Validate(Func<TSuccess, bool> predicate, Func<TFailure> errorFactory)
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
    public TResult Match<TResult>(Func<TSuccess, TResult> onSuccess, Func<TFailure, TResult> onFailure)
    {
        return IsSuccess ? onSuccess(Value!) : onFailure(Error!);
    }

    /// <summary>
    /// Executes an action if the result is successful.
    /// </summary>
    /// <param name="action">The action to execute on the success value.</param>
    /// <returns>The original result.</returns>
    public Result<TSuccess, TFailure> OnSuccess(Action<TSuccess> action)
    {
        if (IsSuccess) action(Value!);
        return this;
    }

    /// <summary>
    /// Executes an action if the result is a failure.
    /// </summary>
    /// <param name="action">The action to execute on the failure error.</param>
    /// <returns>The original result.</returns>
    public Result<TSuccess, TFailure> OnError(Action<TFailure> action)
    {
        if (!IsSuccess) action(Error!);
        return this;
    }
}

/// <summary>
/// Represents the result of an operation with Exception as the failure type.
/// </summary>
/// <typeparam name="TSuccess">The type of the success value.</typeparam>
public class Result<TSuccess> : Result<TSuccess, Exception>
    where TSuccess : notnull
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Result{TSuccess}"/> class with a success value.
    /// </summary>
    /// <param name="value">The success value.</param>
    protected Result(TSuccess value) : base(value)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Result{TSuccess}"/> class with an exception.
    /// </summary>
    /// <param name="exception">The exception representing the failure.</param>
    protected Result(Exception exception) : base(exception)
    {
    }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    /// <param name="value">The success value.</param>
    /// <returns>A result representing a successful operation.</returns>
    public new static Result<TSuccess> Success(TSuccess value) => new(value);

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    /// <param name="exception">The exception representing the failure.</param>
    /// <returns>A result representing a failed operation.</returns>
    public new static Result<TSuccess> Failure(Exception exception) => new(exception);
}
