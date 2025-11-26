using CQReetMediator.Abstractions;

namespace CQReetMediator;

/// <summary>
/// Default implementation of the Mediator pattern.
/// Coordinates the execution of requests, commands, queries, and notifications.
/// </summary>
public class Mediator : IMediator {
    private readonly IServiceProvider _serviceProvider;
    private readonly MediatorRegistry _registry;

    /// <summary>
    /// Initializes a new instance of the <see cref="Mediator"/> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider used to resolve handlers and pipelines.</param>
    /// <param name="registry">The registry containing pre-compiled wrappers for request handling.</param>
    public Mediator(IServiceProvider serviceProvider, MediatorRegistry registry) {
        _serviceProvider = serviceProvider;
        _registry = registry;
    }

    /// <summary>
    /// Sends a request and returns a response using <see cref="ValueTask"/>.
    /// Optimized for synchronous or fast-completing operations.
    /// </summary>
    /// <typeparam name="TResponse">The type of the response.</typeparam>
    /// <param name="request">The request object to be handled.</param>
    /// <param name="ct">An optional cancellation token.</param>
    /// <returns>A <see cref="ValueTask{TResponse}"/> representing the result of the operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown if the request is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown if no handler is registered for the request type.</exception>
    public ValueTask<TResponse?> Send<TResponse>(IRequest<TResponse> request, CancellationToken ct = default) {
        ArgumentNullException.ThrowIfNull(request);
        var wrapper = _registry.GetRequestWrapper<TResponse>(request.GetType());
        return wrapper.Handle(request, _serviceProvider, ct);
    }

    /// <summary>
    /// Sends a request and returns a response using <see cref="Task"/>.
    /// Designed for I/O-bound or long-running asynchronous operations.
    /// </summary>
    /// <typeparam name="TResponse">The type of the response.</typeparam>
    /// <param name="request">The request object to be handled.</param>
    /// <param name="ct">An optional cancellation token.</param>
    /// <returns>A <see cref="Task{TResponse}"/> representing the result of the operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown if the request is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown if no handler is registered for the request type.</exception>
    public Task<TResponse?> SendAsync<TResponse>(IRequest<TResponse> request, CancellationToken ct = default) {
        ArgumentNullException.ThrowIfNull(request);
        var wrapper = _registry.GetAsyncRequestWrapper<TResponse>(request.GetType());
        return wrapper.Handle(request, _serviceProvider, ct);
    }

    /// <summary>
    /// Publishes a notification to all registered handlers.
    /// </summary>
    /// <param name="notification">The notification object to publish.</param>
    /// <param name="ct">An optional cancellation token.</param>
    /// <returns>A <see cref="Task"/> representing the completion of all handlers.</returns>
    /// <exception cref="ArgumentNullException">Thrown if the notification is null.</exception>
    public Task PublishAsync(INotification notification, CancellationToken ct = default) {
        ArgumentNullException.ThrowIfNull(notification);
        var wrapper = _registry.GetNotificationWrapper(notification.GetType());
        // If no handlers are registered, return a completed task immediately.
        return wrapper?.Handle(notification, _serviceProvider, ct) ?? Task.CompletedTask;
    }
}