namespace CQReetMediator.Abstractions;

/// <summary>
/// Represents a request without a return value in the Mediator/CQRS pattern
/// </summary>
public interface IRequest;

/// <summary>
/// Represents a request that returns a response of type TResponse
/// </summary>
/// <typeparam name="TResponse">The type of response data</typeparam>
public interface IRequest<TResponse>;