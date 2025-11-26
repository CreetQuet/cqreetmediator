using CQReetMediator.Abstractions;

namespace CQReetMediator;

/// <summary>
/// Abstract base class for synchronous request wrappers. 
/// Allows storage of generic wrappers in a typed-agnostic dictionary.
/// </summary>
/// <typeparam name="TResponse">The response type.</typeparam>
public abstract class RequestWrapperBase<TResponse> {
    /// <summary>
    /// Handles the request logic.
    /// </summary>
    /// <param name="request">The request object.</param>
    /// <param name="provider">The service provider.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A ValueTask containing the response.</returns>
    public abstract ValueTask<TResponse> Handle(
        object request,
        IServiceProvider provider,
        CancellationToken ct);
}

/// <summary>
/// Abstract base class for asynchronous request wrappers.
/// </summary>
/// <typeparam name="TResponse">The response type.</typeparam>
public abstract class AsyncRequestWrapperBase<TResponse> {
    /// <summary>
    /// Handles the asynchronous request logic.
    /// </summary>
    /// <param name="request">The request object.</param>
    /// <param name="provider">The service provider.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A Task containing the response.</returns>
    public abstract Task<TResponse> Handle(
        object request,
        IServiceProvider provider,
        CancellationToken ct);
}

/// <summary>
/// Abstract base class for notification wrappers.
/// </summary>
public abstract class NotificationWrapperBase {
    /// <summary>
    /// Handles the notification logic.
    /// </summary>
    /// <param name="notification">The notification object.</param>
    /// <param name="provider">The service provider.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A Task representing the operation.</returns>
    public abstract Task Handle(
        INotification notification,
        IServiceProvider provider,
        CancellationToken ct);
}