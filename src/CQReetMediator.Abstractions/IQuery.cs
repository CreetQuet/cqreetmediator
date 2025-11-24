namespace CQReetMediator.Abstractions;

/// <summary>
/// Represents a query in the CQRS pattern (without return value)
/// </summary>
public interface IQuery : IRequest;

/// <summary>
/// Represents a query in the CQRS pattern that returns a response
/// </summary>
/// <typeparam name="TRequest">The type of query data</typeparam>
public interface IQuery<TRequest> : IRequest<TRequest>;