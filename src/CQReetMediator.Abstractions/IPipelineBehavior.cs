namespace CQReetMediator.Abstractions;

/// <summary>
/// Pipeline behavior for intercepting and processing requests (ValueTask - fast operations)
/// </summary>
/// <typeparam name="TRequest">The type of request</typeparam>
/// <typeparam name="TResponse">The type of response</typeparam>
public interface IPipelineBehavior<in TRequest, TResponse> where TRequest : IRequest<TResponse> {
    /// <summary>
    /// Handles the request with pipeline behavior using ValueTask
    /// </summary>
    /// <param name="request">The request to process</param>
    /// <param name="next">The next delegate in the pipeline</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>A ValueTask containing the response</returns>
    ValueTask<TResponse> InvokeAsync(TRequest request, Func<ValueTask<TResponse>> next, CancellationToken ct);
}

/// <summary>
/// Async pipeline behavior for I/O-intensive operations (Task)
/// </summary>
/// <typeparam name="TRequest">The type of request</typeparam>
/// <typeparam name="TResponse">The type of response</typeparam>
public interface IAsyncPipelineBehavior<in TRequest, TResponse> where TRequest : IRequest<TResponse> {
    /// <summary>
    /// Handles the request with pipeline behavior using Task
    /// </summary>
    /// <param name="request">The request to process</param>
    /// <param name="next">The next delegate in the pipeline</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>A Task containing the response</returns>
    Task<TResponse> InvokeAsync(TRequest request, Func<Task<TResponse>> next, CancellationToken ct);
}