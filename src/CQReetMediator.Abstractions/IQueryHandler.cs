namespace CQReetMediator.Abstractions;

/// <summary>
/// Handler for queries with return values using ValueTask
/// </summary>
/// <typeparam name="TRequest">The type of command to handle</typeparam>
/// <typeparam name="TResponse">The type of expected response</typeparam>
public interface IQueryHandler<in TRequest, TResponse> : IRequestHandler<TRequest, TResponse>
    where TRequest : IRequest<TResponse>;

/// <summary>
/// Async handler for queries with return values using Task (I/O-intensive)
/// </summary>
/// <typeparam name="TRequest">The type of command to handle</typeparam>
/// <typeparam name="TResponse">The type of expected response</typeparam>
public interface IAsyncQueryHandler<in TRequest, TResponse> : IAsyncRequestHandler<TRequest, TResponse>
    where TRequest : IRequest<TResponse>;