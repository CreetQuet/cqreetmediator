namespace CQReetMediator.Abstractions;

/// <summary>
/// Handler for requests with return values (queries) using ValueTask
/// </summary>
/// <typeparam name="TRequest">The type of request to handle</typeparam>
/// <typeparam name="TResponse">The type of expected response</typeparam>
public interface IRequestHandler<in TRequest, TResponse> where TRequest : IRequest<TResponse> {
    /// <summary>
    /// Processes a request and returns a response using ValueTask (optimized for fast operations)
    /// </summary>
    /// <param name="request">The request to handle</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>A ValueTask containing the response</returns>
    ValueTask<TResponse> HandleAsync(TRequest request, CancellationToken ct);
}

/// <summary>
/// Async handler for requests with return values using Task (I/O-bound operations)
/// </summary>
/// <typeparam name="TRequest">The type of request to handle</typeparam>
/// <typeparam name="TResponse">The type of expected response</typeparam>
public interface IAsyncRequestHandler<in TRequest, TResponse> where TRequest : IRequest<TResponse> {
    /// <summary>
    /// Processes a request asynchronously and returns a response using Task
    /// </summary>
    /// <param name="request">The request to handle</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>A Task containing the response</returns>
    Task<TResponse> HandleAsync(TRequest request, CancellationToken ct);
}