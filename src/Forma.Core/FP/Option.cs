namespace Forma.Core.FP;

/// <summary>
/// Represents an option type that can either have a value of type <typeparamref name="T"/> (Some) or no value (None).
/// </summary>
/// <typeparam name="T">The type of the value.</typeparam>
public abstract class Option<T>
{
    /// <summary>
    /// Creates an Option with a value.
    /// </summary>
    /// <param name="value">The value to wrap in an Option.</param>
    /// <returns>An Option containing the value.</returns>
    public static Option<T> Some(T value) => new SomeOption(value);
    /// <summary>
    /// Creates an Option with no value.
    /// </summary>
    /// <returns>An Option with no value.</returns>
    public static Option<T> None() => new NoneOption();
    /// <summary>
    /// Creates an Option from a nullable value.
    /// </summary>
    /// <param name="value">The nullable value.</param>
    /// <returns>An Option containing the value if it is not null, otherwise an Option with no value.</returns>
    public static Option<T> From(T? value) => value is null ? None() : Some(value);

    /// <summary>
    /// Gets a value indicating whether the Option has a value.
    /// </summary>
    public abstract bool IsSome { get; }

    /// <summary>
    /// Gets a value indicating whether the Option has no value.
    /// </summary>
    public bool IsNone => !IsSome;

    /// <summary>
    /// Applies a function to the value of the Option if it exists, returning a new Option.
    /// </summary>
    /// <typeparam name="TResult">The type of the result Option.</typeparam>
    /// <param name="binder">The function to apply to the value.</param>
    /// <returns>A new Option with the result of the function, or None if the original Option was None.</returns>
    public abstract Option<TResult> Then<TResult>(Func<T, Option<TResult>> binder);

    /// <summary>
    /// Applies an asynchronous function to the value of the Option if it exists, returning a new Option.
    /// </summary>
    /// <typeparam name="TResult">The type of the result Option.</typeparam>
    /// <param name="binder">The asynchronous function to apply to the value.</param>
    /// <returns>A task containing a new Option with the result of the function, or None if the original Option was None.</returns>
    public abstract Task<Option<TResult>> ThenAsync<TResult>(Func<T, Task<Option<TResult>>> binder);

    /// <summary>
    /// Executes an action on the value of the Option if it exists, returning the same Option.
    /// </summary>
    /// <param name="action">The action to execute on the value.</param>
    /// <returns>The same Option instance.</returns>
    public abstract Option<T> Do(Action<T> action);
    /// <summary>
    /// Executes an asynchronous action on the value of the Option if it exists, returning the same Option.
    /// </summary>
    /// <param name="action">The asynchronous action to execute on the value.</param>
    /// <returns>A task containing the same Option instance.</returns>
    public abstract Task<Option<T>> DoAsync(Func<T, Task> action);

    /// <summary>
    /// Filters the Option based on a predicate, returning None if the predicate fails.
    /// </summary>
    /// <param name="predicate">The predicate to test the value against.</param>
    /// <returns>The same Option if the predicate succeeds and the Option has a value, otherwise None.</returns>
    public abstract Option<T> Validate(Func<T, bool> predicate);
    /// <summary>
    /// Filters the Option based on an asynchronous predicate, returning None if the predicate fails.
    /// </summary>
    /// <param name="predicate">The asynchronous predicate to test the value against.</param>
    /// <returns>A task containing the same Option if the predicate succeeds and the Option has a value, otherwise None.</returns>
    public abstract Task<Option<T>> ValidateAsync(Func<T, Task<bool>> predicate);

    /// <summary>
    /// Matches the Option against two functions, executing the appropriate one based on whether the Option has a value.
    /// </summary>
    /// <typeparam name="TResult">The type of the result.</typeparam>
    /// <param name="some">The function to execute if the Option has a value.</param>
    /// <param name="none">The function to execute if the Option has no value.</param>
    /// <returns>The result of the executed function.</returns>
    public abstract TResult Match<TResult>(Func<T, TResult> some, Func<TResult> none);

    private sealed class SomeOption : Option<T>
    {
        private readonly T _value;
        public SomeOption(T value) => _value = value;

        public override bool IsSome => true;

        public override Option<TResult> Then<TResult>(Func<T, Option<TResult>> binder) => binder(_value);
        public override async Task<Option<TResult>> ThenAsync<TResult>(Func<T, Task<Option<TResult>>> binder) => await binder(_value);

        public override Option<T> Do(Action<T> action)
        {
            action(_value);
            return this;
        }

        public override async Task<Option<T>> DoAsync(Func<T, Task> action)
        {
            await action(_value);
            return this;
        }

        public override Option<T> Validate(Func<T, bool> predicate) => predicate(_value) ? this : None();

        public override async Task<Option<T>> ValidateAsync(Func<T, Task<bool>> predicate) => await predicate(_value) ? this : None();

        public override TResult Match<TResult>(Func<T, TResult> some, Func<TResult> none) => some(_value);
    }

    private sealed class NoneOption : Option<T>
    {
        public override bool IsSome => false;

        public override Option<TResult> Then<TResult>(Func<T, Option<TResult>> binder) => Option<TResult>.None();
        public override Task<Option<TResult>> ThenAsync<TResult>(Func<T, Task<Option<TResult>>> binder) => Task.FromResult(Option<TResult>.None());

        public override Option<T> Do(Action<T> action) => this;
        public override Task<Option<T>> DoAsync(Func<T, Task> action) => Task.FromResult<Option<T>>(this);

        public override Option<T> Validate(Func<T, bool> predicate) => this;
        public override Task<Option<T>> ValidateAsync(Func<T, Task<bool>> predicate) => Task.FromResult<Option<T>>(this);

        public override TResult Match<TResult>(Func<T, TResult> some, Func<TResult> none) => none();
    }
}