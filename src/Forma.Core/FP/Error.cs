namespace Forma.Core.FP;

/// <summary>
/// Base record for all error types in the functional programming model.
/// Errors are immutable and represent failure states without exceptions.
/// </summary>
/// <param name="Message">The error message.</param>
/// <param name="Code">The error code identifier.</param>
public abstract record Error(string Message, string Code)
{
    /// <summary>
    /// Optional metadata associated with the error.
    /// </summary>
    public Dictionary<string, object>? Metadata { get; init; }

    // Factory Methods

    /// <summary>
    /// Creates a generic error with a custom message.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <returns>A GenericError instance.</returns>
    public static GenericError Generic(string message) => new(message);

    /// <summary>
    /// Creates a validation error with a single field error.
    /// </summary>
    /// <param name="field">The field name.</param>
    /// <param name="error">The error message for the field.</param>
    /// <returns>A ValidationError instance.</returns>
    public static ValidationError Validation(string field, string error)
        => new("Validation failed", new Dictionary<string, string[]> { [field] = [error] });

    /// <summary>
    /// Creates a validation error from multiple field errors.
    /// </summary>
    /// <param name="errors">Tuples of field name and error message.</param>
    /// <returns>A ValidationError instance.</returns>
    public static ValidationError Validation(params (string Field, string Error)[] errors)
        => new("Validation failed",
            errors.GroupBy(x => x.Field)
                  .ToDictionary(g => g.Key, g => g.Select(x => x.Error).ToArray()));

    /// <summary>
    /// Creates a validation error from a dictionary of field errors.
    /// </summary>
    /// <param name="errors">Dictionary of field names to error messages.</param>
    /// <param name="message">Optional overall message.</param>
    /// <returns>A ValidationError instance.</returns>
    public static ValidationError Validation(
        Dictionary<string, string[]> errors,
        string message = "Validation failed")
        => new(message, errors);

    /// <summary>
    /// Creates a NotFoundError for a specific entity type and identifier.
    /// </summary>
    /// <typeparam name="TEntity">The type of the entity.</typeparam>
    /// <param name="id">The entity identifier.</param>
    /// <returns>A NotFoundError instance.</returns>
    public static NotFoundError NotFound<TEntity>(object id)
        => new(typeof(TEntity).Name, id);

    /// <summary>
    /// Creates a NotFoundError with a custom entity name.
    /// </summary>
    /// <param name="entityName">The name of the entity.</param>
    /// <param name="id">The entity identifier.</param>
    /// <returns>A NotFoundError instance.</returns>
    public static NotFoundError NotFound(string entityName, object id)
        => new(entityName, id);

    /// <summary>
    /// Creates a BusinessRuleViolationError.
    /// </summary>
    /// <param name="ruleName">The name of the violated business rule.</param>
    /// <param name="message">The error message.</param>
    /// <returns>A BusinessRuleViolationError instance.</returns>
    public static BusinessRuleViolationError BusinessRule(string ruleName, string message)
        => new(ruleName, message);

    /// <summary>
    /// Creates a ConflictError.
    /// </summary>
    /// <param name="message">The conflict message.</param>
    /// <param name="resourceId">Optional resource identifier.</param>
    /// <returns>A ConflictError instance.</returns>
    public static ConflictError Conflict(string message, string? resourceId = null)
        => new(message, resourceId);

    /// <summary>
    /// Creates a ConcurrencyError.
    /// </summary>
    /// <param name="resourceType">The resource type.</param>
    /// <param name="resourceId">The resource identifier.</param>
    /// <returns>A ConcurrencyError instance.</returns>
    public static ConcurrencyError Concurrency(string resourceType, object resourceId)
        => new(resourceType, resourceId);

    /// <summary>
    /// Creates a DataFormatError.
    /// </summary>
    /// <param name="fieldName">The field name.</param>
    /// <param name="expectedFormat">The expected format.</param>
    /// <param name="actualValue">The actual value provided.</param>
    /// <returns>A DataFormatError instance.</returns>
    public static DataFormatError DataFormat(string fieldName, string expectedFormat, string actualValue)
        => new(fieldName, expectedFormat, actualValue);

    /// <summary>
    /// Creates an ExternalServiceError.
    /// </summary>
    /// <param name="serviceName">The external service name.</param>
    /// <param name="message">The error message.</param>
    /// <param name="statusCode">Optional HTTP status code.</param>
    /// <returns>An ExternalServiceError instance.</returns>
    public static ExternalServiceError ExternalService(
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
    public static AggregateError Aggregate(
        IEnumerable<Error> errors,
        string message = "Multiple errors occurred")
        => new(message, errors.ToList());
}

/// <summary>
/// Represents a generic error with a custom message.
/// </summary>
/// <param name="Message">The error message.</param>
public sealed record GenericError(string Message) 
    : Error(Message, "ERROR");

/// <summary>
/// Represents a validation error with field-level error messages.
/// </summary>
/// <param name="Message">The overall validation error message.</param>
/// <param name="Errors">Dictionary of field names to their error messages.</param>
public sealed record ValidationError(
    string Message, 
    IReadOnlyDictionary<string, string[]> Errors
) : Error(Message, "VALIDATION_ERROR");

/// <summary>
/// Represents an error when an entity is not found.
/// </summary>
/// <param name="EntityName">The name of the entity type.</param>
/// <param name="EntityId">The identifier of the entity.</param>
public sealed record NotFoundError(
    string EntityName, 
    object EntityId
) : Error($"{EntityName} with id '{EntityId}' not found", "NOT_FOUND");

/// <summary>
/// Represents an unauthorized access error.
/// </summary>
/// <param name="Message">The unauthorized error message.</param>
public sealed record UnauthorizedError(string Message = "Unauthorized") 
    : Error(Message, "UNAUTHORIZED");

/// <summary>
/// Represents a conflict error (e.g., duplicate key, resource already exists).
/// </summary>
/// <param name="Message">The conflict error message.</param>
/// <param name="ResourceId">Optional resource identifier that caused the conflict.</param>
public sealed record ConflictError(string Message, string? ResourceId = null) 
    : Error(Message, "CONFLICT");

/// <summary>
/// Represents a business rule violation error.
/// </summary>
/// <param name="RuleName">The name of the business rule that was violated.</param>
/// <param name="Message">The error message describing the violation.</param>
public sealed record BusinessRuleViolationError(
    string RuleName,
    string Message
) : Error(Message, $"BUSINESS_RULE_{RuleName.ToUpperInvariant()}");

/// <summary>
/// Represents an error from an external service or API.
/// </summary>
/// <param name="ServiceName">The name of the external service.</param>
/// <param name="Message">The error message.</param>
/// <param name="StatusCode">Optional HTTP status code from the external service.</param>
public sealed record ExternalServiceError(
    string ServiceName,
    string Message,
    int? StatusCode = null
) : Error($"{ServiceName}: {Message}", "EXTERNAL_SERVICE_ERROR")
{
    /// <summary>
    /// Optional timeout duration if the error was due to a timeout.
    /// </summary>
    public TimeSpan? Timeout { get; init; }
}

/// <summary>
/// Represents a concurrency conflict error (e.g., optimistic concurrency violation).
/// </summary>
/// <param name="ResourceType">The type of the resource.</param>
/// <param name="ResourceId">The identifier of the resource.</param>
public sealed record ConcurrencyError(
    string ResourceType,
    object ResourceId
) : Error($"Concurrency conflict on {ResourceType} {ResourceId}", "CONCURRENCY_ERROR");

/// <summary>
/// Represents a data format error (e.g., invalid date format, malformed JSON).
/// </summary>
/// <param name="FieldName">The name of the field with invalid format.</param>
/// <param name="ExpectedFormat">The expected format.</param>
/// <param name="ActualValue">The actual value that was provided.</param>
public sealed record DataFormatError(
    string FieldName,
    string ExpectedFormat,
    string ActualValue
) : Error($"Invalid format for {FieldName}. Expected {ExpectedFormat}, got '{ActualValue}'", "DATA_FORMAT_ERROR");

/// <summary>
/// Represents an aggregate of multiple errors.
/// </summary>
/// <param name="Message">The overall error message.</param>
/// <param name="InnerErrors">The collection of inner errors.</param>
public sealed record AggregateError(
    string Message,
    IReadOnlyList<Error> InnerErrors
) : Error(Message, "AGGREGATE_ERROR");
