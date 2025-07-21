namespace Forma.Abstractions;

/// <summary>
/// Represents a request that expects a response.
/// </summary>
/// <typeparam name="TResponse">The type of the response.</typeparam>
public interface IRequest<out TResponse> : IBaseRequest
{
}

/// <summary>
/// Represents a request that does not expect a response.
/// </summary>
public interface IRequest : IBaseRequest
{
}

/// <summary>
/// Represents a base request interface.
/// </summary>
public interface IBaseRequest { }