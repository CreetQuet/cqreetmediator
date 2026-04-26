using System.Runtime.CompilerServices;
using CQReetMediator.Abstractions;

namespace CQReetMediator;

public sealed class VoidRequestWrapper<TRequest> : RequestWrapperBase
    where TRequest : IRequest {

    public override Task Handle(object request, IServiceProvider provider, CancellationToken ct) {
        var typedRequest = (TRequest)request;

        var handlerObj = provider.GetService(typeof(IRequestHandler<TRequest>));
        if (handlerObj is not IRequestHandler<TRequest> handler)
            throw new InvalidOperationException($"Handler not found for {typeof(TRequest).Name}");

        var behaviorsObj = provider.GetService(typeof(IEnumerable<IPipelineBehavior<TRequest>>));

        if (!HasItems(behaviorsObj))
            return handler.HandleAsync(typedRequest, ct);

        RequestHandlerDelegate executionPlan = () => handler.HandleAsync(typedRequest, ct);

        if (behaviorsObj is IList<IPipelineBehavior<TRequest>> list) {
            for (int i = list.Count - 1; i >= 0; i--) {
                var behavior = list[i];
                var next = executionPlan;
                executionPlan = () => behavior.InvokeAsync(typedRequest, next, ct);
            }
        }

        return executionPlan();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool HasItems(object? collectionObj) {
        if (collectionObj is null) return false;
        if (collectionObj is System.Collections.ICollection c) return c.Count > 0;
        return true;
    }
}

public sealed class RequestWrapper<TRequest, TResponse> : RequestWrapperBase<TResponse>
    where TRequest : IRequest<TResponse> {

    public override Task<TResponse?> Handle(object request, IServiceProvider provider, CancellationToken ct) {
        var typedRequest = (TRequest)request;

        var handlerObj = provider.GetService(typeof(IRequestHandler<TRequest, TResponse>));
        if (handlerObj is not IRequestHandler<TRequest, TResponse> handler)
            throw new InvalidOperationException($"Handler not found for {typeof(TRequest).Name}");

        var behaviorsObj = provider.GetService(typeof(IEnumerable<IPipelineBehavior<TRequest, TResponse>>));

        if (!HasItems(behaviorsObj))
            return handler.HandleAsync(typedRequest, ct);

        RequestHandlerDelegate<TResponse> executionPlan = () => handler.HandleAsync(typedRequest, ct);

        if (behaviorsObj is IList<IPipelineBehavior<TRequest, TResponse>> list) {
            for (int i = list.Count - 1; i >= 0; i--) {
                var behavior = list[i];
                var next = executionPlan;
                executionPlan = () => behavior.InvokeAsync(typedRequest, next, ct);
            }
        }

        return executionPlan();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool HasItems(object? collectionObj) {
        if (collectionObj is null) return false;
        if (collectionObj is System.Collections.ICollection c) return c.Count > 0;
        return true;
    }
}

public sealed class NotificationWrapper<TNotification> : NotificationWrapperBase
    where TNotification : INotification {

    public override async Task Handle(INotification notification, IServiceProvider provider, CancellationToken ct) {
        var typedNotification = (TNotification)notification;
        var handlersObj = provider.GetService(typeof(IEnumerable<INotificationHandler<TNotification>>));

        if (handlersObj is IEnumerable<INotificationHandler<TNotification>> handlers) {
            foreach (var handler in handlers)
                await handler.HandleAsync(typedNotification, ct);
        }
    }
}
