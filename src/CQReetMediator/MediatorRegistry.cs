using System.Collections.Frozen;
namespace CQReetMediator;

public sealed class MediatorRegistry(
    IDictionary<Type, object> requestWrappers,
    IDictionary<Type, object> voidRequestWrappers,
    IDictionary<Type, NotificationWrapperBase> notificationWrappers
) {
    private readonly FrozenDictionary<Type, object> _requestWrappers = requestWrappers.ToFrozenDictionary();
    private readonly FrozenDictionary<Type, object> _voidRequestWrappers = voidRequestWrappers.ToFrozenDictionary();
    private readonly FrozenDictionary<Type, NotificationWrapperBase> _notificationWrappers = notificationWrappers.ToFrozenDictionary();

    public RequestWrapperBase<TResponse> GetRequestWrapper<TResponse>(Type requestType) {
        if (_requestWrappers.TryGetValue(requestType, out var wrapper))
            return (RequestWrapperBase<TResponse>)wrapper;
        throw new InvalidOperationException($"Handler not registered for: {requestType.Name}");
    }

    public RequestWrapperBase GetVoidRequestWrapper(Type requestType) {
        if (_voidRequestWrappers.TryGetValue(requestType, out var wrapper))
            return (RequestWrapperBase)wrapper;
        throw new InvalidOperationException($"Handler not registered for: {requestType.Name}");
    }

    public NotificationWrapperBase? GetNotificationWrapper(Type notificationType) {
        _notificationWrappers.TryGetValue(notificationType, out var wrapper);
        return wrapper;
    }
}
