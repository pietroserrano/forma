namespace Forma.Core.FP;

/// <summary>
/// Extension methods for manipulating Error instances.
/// </summary>
public static class ErrorExtensions
{
    /// <summary>
    /// Adds metadata to an error instance.
    /// </summary>
    /// <typeparam name="TError">The type of error.</typeparam>
    /// <param name="error">The error instance.</param>
    /// <param name="key">The metadata key.</param>
    /// <param name="value">The metadata value.</param>
    /// <returns>A new error instance with the added metadata.</returns>
    public static TError WithMetadata<TError>(this TError error, string key, object? value)
        where TError : Error
    {
        var metadata = error.Metadata ?? new Dictionary<string, object>();
        var newMetadata = new Dictionary<string, object>(metadata);
        
        if (value is not null)
            newMetadata[key] = value;
        
        return error with { Metadata = newMetadata };
    }

    /// <summary>
    /// Combines two validation errors into a single ValidationError.
    /// </summary>
    /// <param name="first">The first validation error.</param>
    /// <param name="second">The second validation error.</param>
    /// <returns>A combined ValidationError.</returns>
    public static ValidationError Combine(this ValidationError first, ValidationError second)
    {
        var combined = new Dictionary<string, string[]>(first.Errors.Count);

        // Seed with cloned arrays from the first error to avoid sharing mutable references.
        foreach (var (key, values) in first.Errors)
            combined[key] = (string[])values.Clone();

        // Merge in errors from the second error, cloning arrays when adding new keys.
        foreach (var (key, values) in second.Errors)
        {
            if (combined.ContainsKey(key))
                combined[key] = combined[key].Concat(values).ToArray();
            else
                combined[key] = (string[])values.Clone();
        }

        return new ValidationError("Multiple validation errors", combined);
    }
}
