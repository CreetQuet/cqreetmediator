using CQReetMediator.Abstractions;

namespace CQReetMediator;

public sealed class VoidRequestWrapper<TRequest> : RequestWrapperBase
    where TRequest : IRequest
{
    private readonly bool _hasPre;
    private readonly bool _hasPipe;
    private readonly bool _hasPost;

    public VoidRequestWrapper(bool hasPre, bool hasPipe, bool hasPost)
    {
        _hasPre = hasPre;
        _hasPipe = hasPipe;
        _hasPost = hasPost;
    }

    public override Task Handle(object request, IServiceProvider provider, CancellationToken ct)
    {
        var typedRequest = (TRequest)request;

        var handlerObj = provider.GetService(typeof(IRequestHandler<TRequest>));
        if (handlerObj is null)
            throw new InvalidOperationException($"Handler not found for {typeof(TRequest).Name}");

        var handler = (IRequestHandler<TRequest>)handlerObj;

        if (!_hasPre && !_hasPipe && !_hasPost)
        {
            return handler.HandleAsync(typedRequest, ct);
        }

        if (!_hasPre && !_hasPost)
        {
            var behaviorsObj = provider.GetService(typeof(IEnumerable<IPipelineBehavior<TRequest>>));

            if (behaviorsObj is IList<IPipelineBehavior<TRequest>> list && list.Count > 0)
            {
                RequestHandlerDelegate next = () => handler.HandleAsync(typedRequest, ct);
                for (int i = list.Count - 1; i >= 0; i--)
                {
                    var behavior = list[i];
                    var currentNext = next;
                    next = () => behavior.InvokeAsync(typedRequest, currentNext, ct);
                }

                return next();
            }

            return handler.HandleAsync(typedRequest, ct);
        }

        return HandleWithAllBehaviorsAsync(typedRequest, handler, provider, ct);
    }

    private async Task HandleWithAllBehaviorsAsync(TRequest request, IRequestHandler<TRequest> handler,
        IServiceProvider provider, CancellationToken ct)
    {
        if (_hasPre)
        {
            var preProcessorsObj = provider.GetService(typeof(IEnumerable<IPreProcessorBehavior<TRequest>>));
            if (preProcessorsObj is IList<IPreProcessorBehavior<TRequest>> preList)
            {
                for (int i = 0; i < preList.Count; i++)
                    await preList[i].ProcessAsync(request, ct).ConfigureAwait(false);
            }
            else if (preProcessorsObj is IEnumerable<IPreProcessorBehavior<TRequest>> preProcessors)
            {
                foreach (var pre in preProcessors) await pre.ProcessAsync(request, ct).ConfigureAwait(false);
            }
        }

        Task executionTask;
        if (!_hasPipe)
        {
            executionTask = handler.HandleAsync(request, ct);
        }
        else
        {
            var behaviorsObj = provider.GetService(typeof(IEnumerable<IPipelineBehavior<TRequest>>));
            if (behaviorsObj is IList<IPipelineBehavior<TRequest>> list && list.Count > 0)
            {
                RequestHandlerDelegate next = () => handler.HandleAsync(request, ct);
                for (int i = list.Count - 1; i >= 0; i--)
                {
                    var behavior = list[i];
                    var currentNext = next;
                    next = () => behavior.InvokeAsync(request, currentNext, ct);
                }

                executionTask = next();
            }
            else
            {
                executionTask = handler.HandleAsync(request, ct);
            }
        }

        await executionTask.ConfigureAwait(false);

        if (_hasPost)
        {
            var postProcessorsObj = provider.GetService(typeof(IEnumerable<IPostProcessorBehavior<TRequest>>));
            if (postProcessorsObj is IList<IPostProcessorBehavior<TRequest>> postList)
            {
                for (int i = 0; i < postList.Count; i++)
                    await postList[i].ProcessAsync(request, ct).ConfigureAwait(false);
            }
            else if (postProcessorsObj is IEnumerable<IPostProcessorBehavior<TRequest>> postProcessors)
            {
                foreach (var post in postProcessors) await post.ProcessAsync(request, ct).ConfigureAwait(false);
            }
        }
    }
}

public sealed class RequestWrapper<TRequest, TResponse> : RequestWrapperBase<TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly bool _hasPre;
    private readonly bool _hasPipe;
    private readonly bool _hasPost;

    public RequestWrapper(bool hasPre, bool hasPipe, bool hasPost)
    {
        _hasPre = hasPre;
        _hasPipe = hasPipe;
        _hasPost = hasPost;
    }

    public override Task<TResponse?> Handle(object request, IServiceProvider provider, CancellationToken ct)
    {
        var typedRequest = (TRequest)request;

        var handlerObj = provider.GetService(typeof(IRequestHandler<TRequest, TResponse>));

        if (handlerObj is null)
            throw new InvalidOperationException($"Handler not found for {typeof(TRequest).Name}");

        var handler = (IRequestHandler<TRequest, TResponse>)handlerObj;

        if (!_hasPre && !_hasPipe && !_hasPost)
        {
            return handler.HandleAsync(typedRequest, ct);
        }

        if (!_hasPre && !_hasPost)
        {
            var behaviorsObj = provider.GetService(typeof(IEnumerable<IPipelineBehavior<TRequest, TResponse>>));

            if (behaviorsObj is IList<IPipelineBehavior<TRequest, TResponse>> list && list.Count > 0)
            {
                RequestHandlerDelegate<TResponse> next = () => handler.HandleAsync(typedRequest, ct);
                for (int i = list.Count - 1; i >= 0; i--)
                {
                    var behavior = list[i];
                    var currentNext = next;
                    next = () => behavior.InvokeAsync(typedRequest, currentNext, ct);
                }

                return next();
            }

            return handler.HandleAsync(typedRequest, ct);
        }

        return HandleWithAllBehaviorsAsync(typedRequest, handler, provider, ct);
    }

    private async Task<TResponse?> HandleWithAllBehaviorsAsync(TRequest request,
        IRequestHandler<TRequest, TResponse> handler, IServiceProvider provider, CancellationToken ct)
    {
        if (_hasPre)
        {
            var preProcessorsObj = provider.GetService(typeof(IEnumerable<IPreProcessorBehavior<TRequest, TResponse>>));
            if (preProcessorsObj is IList<IPreProcessorBehavior<TRequest, TResponse>> preList)
            {
                for (int i = 0; i < preList.Count; i++)
                    await preList[i].ProcessAsync(request, ct).ConfigureAwait(false);
            }
            else if (preProcessorsObj is IEnumerable<IPreProcessorBehavior<TRequest, TResponse>> preProcessors)
            {
                foreach (var pre in preProcessors) await pre.ProcessAsync(request, ct).ConfigureAwait(false);
            }
        }

        Task<TResponse?> executionTask;
        if (!_hasPipe)
        {
            executionTask = handler.HandleAsync(request, ct);
        }
        else
        {
            var behaviorsObj = provider.GetService(typeof(IEnumerable<IPipelineBehavior<TRequest, TResponse>>));
            if (behaviorsObj is IList<IPipelineBehavior<TRequest, TResponse>> list && list.Count > 0)
            {
                RequestHandlerDelegate<TResponse> executionPlan = () => handler.HandleAsync(request, ct);
                for (int i = list.Count - 1; i >= 0; i--)
                {
                    var behavior = list[i];
                    var currentNext = executionPlan;
                    executionPlan = () => behavior.InvokeAsync(request, currentNext, ct);
                }

                executionTask = executionPlan();
            }
            else
            {
                executionTask = handler.HandleAsync(request, ct);
            }
        }

        var response = await executionTask.ConfigureAwait(false);

        if (_hasPost)
        {
            var postProcessorsObj =
                provider.GetService(typeof(IEnumerable<IPostProcessorBehavior<TRequest, TResponse>>));
            if (postProcessorsObj is IList<IPostProcessorBehavior<TRequest, TResponse>> postList)
            {
                for (int i = 0; i < postList.Count; i++)
                    await postList[i].ProcessAsync(request, response, ct).ConfigureAwait(false);
            }
            else if (postProcessorsObj is IEnumerable<IPostProcessorBehavior<TRequest, TResponse>> postProcessors)
            {
                foreach (var post in postProcessors)
                    await post.ProcessAsync(request, response, ct).ConfigureAwait(false);
            }
        }

        return response;
    }
}

public sealed class NotificationWrapper<TNotification> : NotificationWrapperBase
    where TNotification : INotification
{
    public override Task Handle(INotification notification, IServiceProvider provider, CancellationToken ct)
    {
        var typedNotification = (TNotification)notification;
        var handlersObj = provider.GetService(typeof(IEnumerable<INotificationHandler<TNotification>>));

        if (handlersObj is IList<INotificationHandler<TNotification>> list)
        {
            if (list.Count == 0) return Task.CompletedTask;
            if (list.Count == 1) return list[0].HandleAsync(typedNotification, ct);

            return HandleMultipleAsync(list, typedNotification, ct);
        }
        else if (handlersObj is IEnumerable<INotificationHandler<TNotification>> handlers)
        {
            return HandleMultipleEnumerableAsync(handlers, typedNotification, ct);
        }

        return Task.CompletedTask;
    }

    private static async Task HandleMultipleAsync(IList<INotificationHandler<TNotification>> list, TNotification notif,
        CancellationToken ct)
    {
        for (int i = 0; i < list.Count; i++) await list[i].HandleAsync(notif, ct).ConfigureAwait(false);
    }

    private static async Task HandleMultipleEnumerableAsync(IEnumerable<INotificationHandler<TNotification>> handlers,
        TNotification notif, CancellationToken ct)
    {
        foreach (var handler in handlers) await handler.HandleAsync(notif, ct).ConfigureAwait(false);
    }
}