namespace CQReetMediator.Abstractions;

/// <summary>
/// Handler for commands with return values using ValueTask
/// </summary>
/// <typeparam name="TRequest">The type of command to handle</typeparam>
/// <typeparam name="TResponse">The type of expected response</typeparam>
public interface ICommandHandler<in TRequest, TResponse> : IRequestHandler<TRequest, TResponse>
    where TRequest : IRequest<TResponse>;

/// <summary>
/// Async handler for commands with return values using Task (I/O-intensive)
/// </summary>
/// <typeparam name="TRequest">The type of command to handle</typeparam>
/// <typeparam name="TResponse">The type of expected response</typeparam>
public interface IAsyncCommandHandler<in TRequest, TResponse> : IAsyncRequestHandler<TRequest, TResponse>
    where TRequest : IRequest<TResponse>;