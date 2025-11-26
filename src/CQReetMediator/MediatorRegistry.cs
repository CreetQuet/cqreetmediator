using System.Collections.Frozen;

namespace CQReetMediator;

/// <summary>
/// Registry that stores pre-compiled wrappers for handling requests and notifications.
/// Uses FrozenDictionary for O(1) high-performance lookups.
/// </summary>
public class MediatorRegistry(
    IDictionary<Type, object> requestWrappers,
    IDictionary<Type, object> asyncRequestWrappers,
    IDictionary<Type, NotificationWrapperBase> notificationWrappers
) {

    private readonly FrozenDictionary<Type, object> _requestWrappers = requestWrappers.ToFrozenDictionary();
    private readonly FrozenDictionary<Type, object> _asyncRequestWrappers = asyncRequestWrappers.ToFrozenDictionary();
    private readonly FrozenDictionary<Type, NotificationWrapperBase> _notificationWrappers = notificationWrappers.ToFrozenDictionary();

    /// <summary>
    /// Retrieves the wrapper for a specific synchronous request type.
    /// </summary>
    /// <typeparam name="TResponse">The response type of the request.</typeparam>
    /// <param name="requestType">The concrete type of the request.</param>
    /// <returns>The <see cref="RequestWrapperBase{TResponse}"/> capable of handling the request.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the request type is not registered.</exception>
    public RequestWrapperBase<TResponse> GetRequestWrapper<TResponse>(Type requestType) {
        if (_requestWrappers.TryGetValue(requestType, out var wrapper)) {
            return (RequestWrapperBase<TResponse>)wrapper;
        }
        throw new InvalidOperationException($"Handler not registered for: {requestType.Name}");
    }

    /// <summary>
    /// Retrieves the wrapper for a specific asynchronous request type.
    /// </summary>
    /// <typeparam name="TResponse">The response type of the request.</typeparam>
    /// <param name="requestType">The concrete type of the request.</param>
    /// <returns>The <see cref="AsyncRequestWrapperBase{TResponse}"/> capable of handling the request.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the request type is not registered.</exception>
    public AsyncRequestWrapperBase<TResponse> GetAsyncRequestWrapper<TResponse>(Type requestType) {
        if (_asyncRequestWrappers.TryGetValue(requestType, out var wrapper)) {
            return (AsyncRequestWrapperBase<TResponse>)wrapper;
        }
        throw new InvalidOperationException($"Async Handler not registered for: {requestType.Name}");
    }

    /// <summary>
    /// Retrieves the wrapper for a specific notification type.
    /// </summary>
    /// <param name="notificationType">The concrete type of the notification.</param>
    /// <returns>The <see cref="NotificationWrapperBase"/> if found; otherwise, null.</returns>
    public NotificationWrapperBase? GetNotificationWrapper(Type notificationType) {
        _notificationWrappers.TryGetValue(notificationType, out var wrapper);
        return wrapper;
    }
}