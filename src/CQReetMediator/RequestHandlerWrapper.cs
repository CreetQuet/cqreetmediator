using CQReetMediator.Abstractions;

namespace CQReetMediator;

/// <summary>
/// Wrapper responsible for executing a specific synchronous request type (<see cref="ValueTask"/>).
/// Handles resolution of the handler and execution of both sync and async pipelines.
/// </summary>
/// <typeparam name="TRequest">The type of the request.</typeparam>
/// <typeparam name="TResponse">The type of the response.</typeparam>
public class RequestWrapper<TRequest, TResponse> : RequestWrapperBase<TResponse>
    where TRequest : IRequest<TResponse> {
    
    /// <summary>
    /// Executes the request by resolving the handler and wrapping it in any registered pipeline behaviors.
    /// </summary>
    /// <param name="request">The request object (passed as object to match abstract base signature).</param>
    /// <param name="provider">The service provider to resolve dependencies.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A <see cref="ValueTask{TResponse}"/> containing the response.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the required handler is not found in the container.</exception>
    public override ValueTask<TResponse?> Handle(
        object request,
        IServiceProvider provider,
        CancellationToken ct
    ) {
        var typedRequest = (TRequest)request;

        var handlerObj = provider.GetService(typeof(IRequestHandler<TRequest, TResponse>));
        if (handlerObj is not IRequestHandler<TRequest, TResponse> handler) {
            throw new InvalidOperationException($"Handler not found for {typeof(TRequest).Name}");
        }

        var syncBehaviorsObj = provider.GetService(typeof(IEnumerable<IPipelineBehavior<TRequest, TResponse>>));
        var asyncBehaviorsObj = provider.GetService(typeof(IEnumerable<IAsyncPipelineBehavior<TRequest, TResponse>>));

        bool hasSync = HasItems(syncBehaviorsObj);
        bool hasAsync = HasItems(asyncBehaviorsObj);

        if (!hasSync && !hasAsync) {
            return handler.HandleAsync(typedRequest, ct);
        }

        RequestHandlerDelegate<TResponse> executionPlan = () => handler.HandleAsync(typedRequest, ct);

        if (hasAsync && asyncBehaviorsObj is IList<IAsyncPipelineBehavior<TRequest, TResponse>> asyncList) {
            for (int i = asyncList.Count - 1; i >= 0; i--) {
                var behavior = asyncList[i];
                var next = executionPlan;
                executionPlan = () => {
                    var taskResponse = behavior.InvokeAsync(typedRequest, () => next().AsTask(), ct);
                    return new ValueTask<TResponse?>(taskResponse);
                };
            }
        } else if (hasAsync && asyncBehaviorsObj is IEnumerable<IAsyncPipelineBehavior<TRequest, TResponse>> asyncEnum) {
            foreach (var behavior in asyncEnum.Reverse()) {
                var next = executionPlan;
                executionPlan = () => {
                    var taskResponse = behavior.InvokeAsync(typedRequest, () => next().AsTask(), ct);
                    return new ValueTask<TResponse?>(taskResponse);
                };
            }
        }

        if (hasSync && syncBehaviorsObj is IList<IPipelineBehavior<TRequest, TResponse>> syncList) {
            for (int i = syncList.Count - 1; i >= 0; i--) {
                var behavior = syncList[i];
                var next = executionPlan;
                executionPlan = () => behavior.InvokeAsync(typedRequest, next, ct);
            }
        } else if (hasSync && syncBehaviorsObj is IEnumerable<IPipelineBehavior<TRequest, TResponse>> syncEnum) {
            foreach (var behavior in syncEnum.Reverse()) {
                var next = executionPlan;
                executionPlan = () => behavior.InvokeAsync(typedRequest, next, ct);
            }
        }

        return executionPlan();
    }

    /// <summary>
    /// Efficiently checks if a collection is null or empty without unnecessary enumeration.
    /// </summary>
    /// <param name="collectionObj">The collection object to check.</param>
    /// <returns>True if the collection contains items; otherwise, false.</returns>
    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
    private static bool HasItems(object? collectionObj) {
        if (collectionObj == null) return false;
        if (collectionObj is System.Collections.ICollection c) return c.Count > 0;
        return true;
    }
}

/// <summary>
/// Wrapper responsible for executing a specific asynchronous request type (<see cref="Task"/>).
/// Handles resolution of the handler and execution of both sync and async pipelines.
/// </summary>
/// <typeparam name="TRequest">The type of the request.</typeparam>
/// <typeparam name="TResponse">The type of the response.</typeparam>
public class AsyncRequestWrapper<TRequest, TResponse> : AsyncRequestWrapperBase<TResponse>
    where TRequest : IRequest<TResponse> {
    public override Task<TResponse?> Handle(
        object request,
        IServiceProvider provider,
        CancellationToken ct
    ) {
        var typedRequest = (TRequest)request;
        var handlerObj = provider.GetService(typeof(IAsyncRequestHandler<TRequest, TResponse>));

        if (handlerObj is not IAsyncRequestHandler<TRequest, TResponse> handler)
            throw new InvalidOperationException($"Async Handler not found for {typeof(TRequest).Name}");

        var syncBehaviorsObj = provider.GetService(typeof(IEnumerable<IPipelineBehavior<TRequest, TResponse>>));
        var asyncBehaviorsObj = provider.GetService(typeof(IEnumerable<IAsyncPipelineBehavior<TRequest, TResponse>>));

        bool hasSync = HasItems(syncBehaviorsObj);
        bool hasAsync = HasItems(asyncBehaviorsObj);

        if (!hasSync && !hasAsync) {
            return handler.HandleAsync(typedRequest, ct);
        }

        RequestHandlerDelegateAsync<TResponse> executionPlan = () => handler.HandleAsync(typedRequest, ct);

        if (hasSync && syncBehaviorsObj is IList<IPipelineBehavior<TRequest, TResponse>> syncList) {
            for (int i = syncList.Count - 1; i >= 0; i--) {
                var behavior = syncList[i];
                var next = executionPlan;
                executionPlan = () => {
                    var vt = behavior.InvokeAsync(typedRequest, () => new ValueTask<TResponse?>(next()), ct);
                    return vt.AsTask();
                };
            }
        }

        if (hasAsync && asyncBehaviorsObj is IList<IAsyncPipelineBehavior<TRequest, TResponse>> asyncList) {
            for (int i = asyncList.Count - 1; i >= 0; i--) {
                var behavior = asyncList[i];
                var next = executionPlan;
                executionPlan = () => behavior.InvokeAsync(typedRequest, next, ct);
            }
        }

        return executionPlan();
    }

    /// <summary>
    /// Efficiently checks if a collection is null or empty without unnecessary enumeration.
    /// </summary>
    /// <param name="collectionObj">The collection object to check.</param>
    /// <returns>True if the collection contains items; otherwise, false.</returns>
    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
    private static bool HasItems(object? collectionObj) {
        if (collectionObj == null) return false;
        if (collectionObj is System.Collections.ICollection c) return c.Count > 0;
        return true;
    }
}

/// <summary>
/// Wrapper responsible for executing notifications.
/// </summary>
/// <typeparam name="TNotification">The type of the notification.</typeparam>
public class NotificationWrapper<TNotification> : NotificationWrapperBase
    where TNotification : INotification {
    
    /// <summary>
    /// Executes all registered handlers for the given notification type sequentially.
    /// </summary>
    /// <param name="notification">The notification object.</param>
    /// <param name="provider">The service provider.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A <see cref="Task"/> representing the completion of all handlers.</returns>
    public override async Task Handle(
        INotification notification,
        IServiceProvider provider,
        CancellationToken ct
    ) {
        var typedNotification = (TNotification)notification;

        var handlersObj = provider.GetService(typeof(IEnumerable<INotificationHandler<TNotification>>));

        if (handlersObj is IEnumerable<INotificationHandler<TNotification>> handlers) {
            foreach (var handler in handlers) {
                await handler.HandleAsync(typedNotification, ct);
            }
        }
    }
}