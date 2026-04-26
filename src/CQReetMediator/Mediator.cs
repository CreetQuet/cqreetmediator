using CQReetMediator.Abstractions;

namespace CQReetMediator;

public sealed class Mediator(IServiceProvider serviceProvider, MediatorRegistry registry) : IMediator {

    public Task SendAsync(IRequest request, CancellationToken ct = default) {
        ArgumentNullException.ThrowIfNull(request);
        var wrapper = registry.GetVoidRequestWrapper(request.GetType());
        return wrapper.Handle(request, serviceProvider, ct);
    }

    public Task<TResponse?> SendAsync<TResponse>(IRequest<TResponse> request, CancellationToken ct = default) {
        ArgumentNullException.ThrowIfNull(request);
        var wrapper = registry.GetRequestWrapper<TResponse>(request.GetType());
        return wrapper.Handle(request, serviceProvider, ct);
    }

    public Task PublishAsync(INotification notification, CancellationToken ct = default) {
        ArgumentNullException.ThrowIfNull(notification);
        var wrapper = registry.GetNotificationWrapper(notification.GetType());
        return wrapper?.Handle(notification, serviceProvider, ct) ?? Task.CompletedTask;
    }
}
