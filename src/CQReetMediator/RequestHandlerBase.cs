using CQReetMediator.Abstractions;

namespace CQReetMediator;

public abstract class RequestWrapperBase {
    public abstract Task Handle(
        object request,
        IServiceProvider provider,
        CancellationToken ct);
}

public abstract class RequestWrapperBase<TResponse> {
    public abstract Task<TResponse?> Handle(
        object request,
        IServiceProvider provider,
        CancellationToken ct);
}

public abstract class NotificationWrapperBase {
    public abstract Task Handle(
        INotification notification,
        IServiceProvider provider,
        CancellationToken ct);
}
