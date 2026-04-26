using System.Runtime.CompilerServices;
using CQReetMediator.Abstractions;

namespace CQReetMediator;

public sealed class VoidRequestWrapper<TRequest> : RequestWrapperBase
    where TRequest : class, IRequest
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
        var typedRequest = Unsafe.As<TRequest>(request);

        var handlerObj = provider.GetService(typeof(IRequestHandler<TRequest>));
        if (handlerObj is null)
            throw new InvalidOperationException($"Handler not found for {typeof(TRequest).Name}");

        var handler = Unsafe.As<IRequestHandler<TRequest>>(handlerObj);

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
            var preProcessors =
                Unsafe.As<IEnumerable<IPreProcessorBehavior<TRequest>>>(
                    provider.GetService(typeof(IEnumerable<IPreProcessorBehavior<TRequest>>)));
            if (preProcessors is IList<IPreProcessorBehavior<TRequest>> preList)
            {
                for (int i = 0; i < preList.Count; i++)
                    await preList[i].ProcessAsync(request, ct).ConfigureAwait(false);
            }
            else
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
            var postProcessors =
                Unsafe.As<IEnumerable<IPostProcessorBehavior<TRequest>>>(
                    provider.GetService(typeof(IEnumerable<IPostProcessorBehavior<TRequest>>)));
            if (postProcessors is IList<IPostProcessorBehavior<TRequest>> postList)
            {
                for (int i = 0; i < postList.Count; i++)
                    await postList[i].ProcessAsync(request, ct).ConfigureAwait(false);
            }
            else
            {
                foreach (var post in postProcessors) await post.ProcessAsync(request, ct).ConfigureAwait(false);
            }
        }
    }
}

public sealed class RequestWrapper<TRequest, TResponse> : RequestWrapperBase<TResponse>
    where TRequest : class, IRequest<TResponse>
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
        var typedRequest = Unsafe.As<TRequest>(request);

        var handlerObj = provider.GetService(typeof(IRequestHandler<TRequest, TResponse>));

        if (handlerObj is null)
            throw new InvalidOperationException($"Handler not found for {typeof(TRequest).Name}");

        var handler = Unsafe.As<IRequestHandler<TRequest, TResponse>>(handlerObj);

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
            var preProcessors =
                Unsafe.As<IEnumerable<IPreProcessorBehavior<TRequest, TResponse>>>(
                    provider.GetService(typeof(IEnumerable<IPreProcessorBehavior<TRequest, TResponse>>)));
            if (preProcessors is IList<IPreProcessorBehavior<TRequest, TResponse>> preList)
            {
                for (int i = 0; i < preList.Count; i++)
                    await preList[i].ProcessAsync(request, ct).ConfigureAwait(false);
            }
            else
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
            var postProcessors =
                Unsafe.As<IEnumerable<IPostProcessorBehavior<TRequest, TResponse>>>(
                    provider.GetService(typeof(IEnumerable<IPostProcessorBehavior<TRequest, TResponse>>)));
            if (postProcessors is IList<IPostProcessorBehavior<TRequest, TResponse>> postList)
            {
                for (int i = 0; i < postList.Count; i++)
                    await postList[i].ProcessAsync(request, response, ct).ConfigureAwait(false);
            }
            else
            {
                foreach (var post in postProcessors)
                    await post.ProcessAsync(request, response, ct).ConfigureAwait(false);
            }
        }

        return response;
    }
}

public sealed class NotificationWrapper<TNotification> : NotificationWrapperBase
    where TNotification : class, INotification
{
    public override async Task Handle(INotification notification, IServiceProvider provider, CancellationToken ct)
    {
        var typedNotification = Unsafe.As<TNotification>(notification);
        var handlersObj = provider.GetService(typeof(IEnumerable<INotificationHandler<TNotification>>));

        if (handlersObj is IList<INotificationHandler<TNotification>> list)
        {
            for (int i = 0; i < list.Count; i++) await list[i].HandleAsync(typedNotification, ct).ConfigureAwait(false);
        }
        else if (handlersObj is IEnumerable<INotificationHandler<TNotification>> handlers)
        {
            foreach (var handler in handlers) await handler.HandleAsync(typedNotification, ct).ConfigureAwait(false);
        }
    }
}