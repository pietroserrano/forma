namespace Forma.Core.FP;

/// <summary>
/// Extension methods for creating and manipulating Error instances.
/// </summary>
public static class ErrorExtensions
{
    /// <summary>
    /// Creates a generic error from a string message.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <returns>A GenericError instance.</returns>
    public static GenericError ToError(this string message) => new(message);

    /// <summary>
    /// Creates a validation error with a single field error.
    /// </summary>
    /// <param name="field">The field name.</param>
    /// <param name="errorMessage">The error message for the field.</param>
    /// <returns>A ValidationError instance.</returns>
    public static ValidationError ToValidationError(this string field, string errorMessage)
        => new("Validation failed", new Dictionary<string, string[]> { [field] = [errorMessage] });

    /// <summary>
    /// Creates a validation error from multiple field errors.
    /// </summary>
    /// <param name="errors">Tuples of field name and error message.</param>
    /// <returns>A ValidationError instance.</returns>
    public static ValidationError ToValidationError(params (string Field, string Error)[] errors)
        => new("Validation failed",
            errors.GroupBy(x => x.Field)
                  .ToDictionary(g => g.Key, g => g.Select(x => x.Error).ToArray()));

    /// <summary>
    /// Creates a validation error from a dictionary of field errors.
    /// </summary>
    /// <param name="errors">Dictionary of field names to error messages.</param>
    /// <param name="message">Optional overall message.</param>
    /// <returns>A ValidationError instance.</returns>
    public static ValidationError ToValidationError(
        this Dictionary<string, string[]> errors, 
        string message = "Validation failed")
        => new(message, errors);

    /// <summary>
    /// Creates a NotFoundError for a specific entity type and identifier.
    /// </summary>
    /// <typeparam name="TEntity">The type of the entity.</typeparam>
    /// <param name="id">The entity identifier.</param>
    /// <returns>A NotFoundError instance.</returns>
    public static NotFoundError ToNotFoundError<TEntity>(this object id)
        => new(typeof(TEntity).Name, id);

    /// <summary>
    /// Creates a NotFoundError with a custom entity name.
    /// </summary>
    /// <param name="entityName">The name of the entity.</param>
    /// <param name="id">The entity identifier.</param>
    /// <returns>A NotFoundError instance.</returns>
    public static NotFoundError ToNotFoundError(string entityName, object id)
        => new(entityName, id);

    /// <summary>
    /// Adds metadata to an error instance.
    /// </summary>
    /// <typeparam name="TError">The type of error.</typeparam>
    /// <param name="error">The error instance.</param>
    /// <param name="key">The metadata key.</param>
    /// <param name="value">The metadata value.</param>
    /// <returns>A new error instance with the added metadata.</returns>
    public static TError WithMetadata<TError>(this TError error, string key, object value)
        where TError : Error
    {
        var metadata = error.Metadata ?? new Dictionary<string, object>();
        var newMetadata = new Dictionary<string, object>(metadata) { [key] = value };
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
        var combined = new Dictionary<string, string[]>(first.Errors);
        foreach (var (key, values) in second.Errors)
        {
            if (combined.ContainsKey(key))
                combined[key] = combined[key].Concat(values).ToArray();
            else
                combined[key] = values;
        }
        return new ValidationError("Multiple validation errors", combined);
    }

    /// <summary>
    /// Creates a BusinessRuleViolationError.
    /// </summary>
    /// <param name="ruleName">The name of the violated business rule.</param>
    /// <param name="message">The error message.</param>
    /// <returns>A BusinessRuleViolationError instance.</returns>
    public static BusinessRuleViolationError ToBusinessRuleError(string ruleName, string message)
        => new(ruleName, message);

    /// <summary>
    /// Creates a ConflictError.
    /// </summary>
    /// <param name="message">The conflict message.</param>
    /// <param name="resourceId">Optional resource identifier.</param>
    /// <returns>A ConflictError instance.</returns>
    public static ConflictError ToConflictError(this string message, string? resourceId = null)
        => new(message, resourceId);

    /// <summary>
    /// Creates a ConcurrencyError.
    /// </summary>
    /// <param name="resourceType">The resource type.</param>
    /// <param name="resourceId">The resource identifier.</param>
    /// <returns>A ConcurrencyError instance.</returns>
    public static ConcurrencyError ToConcurrencyError(string resourceType, object resourceId)
        => new(resourceType, resourceId);

    /// <summary>
    /// Creates a DataFormatError.
    /// </summary>
    /// <param name="fieldName">The field name.</param>
    /// <param name="expectedFormat">The expected format.</param>
    /// <param name="actualValue">The actual value provided.</param>
    /// <returns>A DataFormatError instance.</returns>
    public static DataFormatError ToDataFormatError(string fieldName, string expectedFormat, string actualValue)
        => new(fieldName, expectedFormat, actualValue);

    /// <summary>
    /// Creates an ExternalServiceError.
    /// </summary>
    /// <param name="serviceName">The external service name.</param>
    /// <param name="message">The error message.</param>
    /// <param name="statusCode">Optional HTTP status code.</param>
    /// <returns>An ExternalServiceError instance.</returns>
    public static ExternalServiceError ToExternalServiceError(
        string serviceName, 
        string message, 
        int? statusCode = null)
        => new(serviceName, message, statusCode);

    /// <summary>
    /// Creates an AggregateError from multiple errors.
    /// </summary>
    /// <param name="errors">The collection of errors.</param>
    /// <param name="message">Optional overall message.</param>
    /// <returns>An AggregateError instance.</returns>
    public static AggregateError ToAggregateError(
        this IEnumerable<Error> errors, 
        string message = "Multiple errors occurred")
        => new(message, errors.ToList());
}
