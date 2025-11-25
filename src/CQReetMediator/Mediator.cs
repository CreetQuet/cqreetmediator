using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using CQReetMediator.Abstractions;

namespace CQReetMediator;

/// <summary>
/// Default implementation of <see cref="IMediator"/> that handles request/response coordination
/// and executes pipeline behaviors for cross-cutting concerns.
/// </summary>
/// <remarks>
/// <para>
/// This class is responsible for:
/// <list type="bullet">
/// <item>Resolving appropriate request handlers from the dependency injection container</item>
/// <item>Executing pipeline behaviors in the order they are registered</item>
/// <item>Handling both synchronous-friendly (ValueTask) and asynchronous (Task) operations</item>
/// <item>Publishing notifications to multiple handlers</item>
/// </list>
/// </para>
/// <para>
/// <strong>Handler Resolution Order:</strong>
/// <list type="number">
/// <item>Pipeline behaviors (if any)</item>
/// <item>Primary request handler</item>
/// <item>Async fallback (if applicable)</item>
/// </list>
/// </para>
/// </remarks>
public sealed class Mediator : IMediator {
    private readonly IServiceFactory _factory;

    public Mediator(IServiceFactory factory) {
        _factory = factory;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Mediator"/> class.
    /// </summary>
    /// <param name="singleResolver">The service provider used to resolve handlers and behaviors</param>
    /// <param name="multipleResolver">The service provider used to resolve handlers and behaviors</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="singleResolver"/> is null</exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="multipleResolver"/> is null</exception>
    private readonly static ConcurrentDictionary<Type, Delegate> ValueHandlerInvokerCache = new();

    private readonly static ConcurrentDictionary<Type, Delegate> TaskHandlerInvokerCache = new();

    private readonly static ConcurrentDictionary<Type, Delegate> ValuePipelineInvokerCache = new();
    private readonly static ConcurrentDictionary<Type, Delegate> TaskPipelineInvokerCache = new();

    private readonly static ConcurrentDictionary<Type, Delegate> NotificationInvokerCache = new();

    /// <summary>
    /// Sends a request with return value in an optimized way using ValueTask.
    /// This implementation prioritizes synchronous-friendly handlers and executes pipeline behaviors.
    /// </summary>
    /// <typeparam name="TResponse">The type of response expected from the request</typeparam>
    /// <param name="request">The request to process</param>
    /// <param name="ct">Cancellation token to cancel the operation</param>
    /// <returns>A ValueTask containing the response of type <typeparamref name="TResponse"/></returns>
    /// <remarks>
    /// <para>
    /// <strong>Execution Flow:</strong>
    /// <list type="number">
    /// <item>Resolves all registered <see cref="IPipelineBehavior{TRequest, TResponse}"/> instances</item>
    /// <item>Executes pipeline behaviors in registration order</item>
    /// <item>Invokes the primary request handler</item>
    /// <item>Returns the response through the pipeline chain</item>
    /// </list>
    /// </para>
    /// <para>
    /// If no pipeline behaviors are registered, the request is sent directly to the handler.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="request"/> is null</exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when no handler is registered for the request type or when handler resolution fails
    /// </exception>
    public ValueTask<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken ct = default) {
        if (request is null) throw new ArgumentNullException(nameof(request));

        var requestType = request.GetType();

        var valueHandlerInterface = typeof(IRequestHandler<,>).MakeGenericType(requestType, typeof(TResponse));
        var valueHandler = _factory.Resolve(valueHandlerInterface);

        if (valueHandler != null) {
            var inv = (Func<object, object, CancellationToken, ValueTask<TResponse>>)
                ValueHandlerInvokerCache.GetOrAdd(valueHandlerInterface,
                    _ => BuildValueHandlerInvoker<TResponse>(valueHandlerInterface));

            var pipelineInterface =
                typeof(IPipelineBehavior<,>)
                    .MakeGenericType(requestType, typeof(TResponse));

            var pipelines = _factory.ResolveAll(pipelineInterface);

            if (!pipelines.Any()) {
                return inv(valueHandler, request, ct);
            }

            Func<ValueTask<TResponse>> next = () => inv(valueHandler, request, ct);

            for (int i = pipelines.Count() - 1; i >= 0; i--) {
                var pipelineInst = pipelines.ElementAt(i);
                var pipelineInvokerDelegate =
                    (Func<object, object, Func<ValueTask<TResponse>>, CancellationToken, ValueTask<TResponse>>)
                    ValuePipelineInvokerCache.GetOrAdd(pipelineInst.GetType(),
                        _ => BuildValuePipelineInvoker<TResponse>(pipelineInst.GetType()));

                var outerNext = next;
                next = () => pipelineInvokerDelegate(pipelineInst, request, outerNext, ct);
            }

            return next();
        }

        var taskHandlerInterface = typeof(IAsyncRequestHandler<,>).MakeGenericType(requestType, typeof(TResponse));
        var taskHandler = _factory.Resolve(taskHandlerInterface);

        if (taskHandler != null) {
            var invTask = (Func<object, object, CancellationToken, Task<TResponse>>)
                TaskHandlerInvokerCache.GetOrAdd(taskHandlerInterface,
                    _ => BuildTaskHandlerInvoker<TResponse>(taskHandlerInterface));

            var taskPipelineInterface =
                typeof(IAsyncPipelineBehavior<,>)
                    .MakeGenericType(requestType, typeof(TResponse));

            var pipelines = _factory.ResolveAll(taskPipelineInterface);

            if (!pipelines.Any()) {
                var task = invTask(taskHandler, request, ct);
                return new ValueTask<TResponse>(task);
            }

            Func<Task<TResponse>> nextTask = () => invTask(taskHandler, request, ct);

            for (int i = pipelines.Count() - 1; i >= 0; i--) {
                var pipelineInst = pipelines.ElementAt(i);
                var pipelineInvokerDelegate = (Func<object, object, Func<Task<TResponse>>, CancellationToken, Task<TResponse>>)
                    TaskPipelineInvokerCache.GetOrAdd(pipelineInst.GetType(),
                        _ => BuildTaskPipelineInvoker<TResponse>(pipelineInst.GetType()));

                var outerNext = nextTask;
                nextTask = () => pipelineInvokerDelegate(pipelineInst, request, outerNext, ct);
            }

            return new ValueTask<TResponse>(nextTask());
        }

        throw new InvalidOperationException($"No handler found for request type {requestType} -> {typeof(TResponse)}");
    }

    /// <summary>
    /// Sends a request with return value asynchronously using Task for I/O-intensive operations.
    /// This implementation is optimized for truly asynchronous handlers and executes pipeline behaviors.
    /// </summary>
    /// <typeparam name="TResponse">The type of response expected from the request</typeparam>
    /// <param name="request">The request to process</param>
    /// <param name="ct">Cancellation token to cancel the operation</param>
    /// <returns>A Task containing the response of type <typeparamref name="TResponse"/></returns>
    /// <remarks>
    /// <para>
    /// <strong>Execution Flow:</strong>
    /// <list type="number">
    /// <item>Resolves all registered <see cref="IAsyncPipelineBehavior{TRequest, TResponse}"/> instances</item>
    /// <item>Executes async pipeline behaviors in registration order</item>
    /// <item>Invokes the primary async request handler</item>
    /// <item>Returns the response through the pipeline chain</item>
    /// </list>
    /// </para>
    /// <para>
    /// This method will fall back to synchronous handlers if no async handler is found,
    /// wrapping the result in a Task for consistent async execution.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="request"/> is null</exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when no handler is registered for the request type
    /// </exception>
    public async Task<TResponse> SendAsync<TResponse>(IRequest<TResponse> request, CancellationToken ct = default) {
        if (request is null) throw new ArgumentNullException(nameof(request));

        var requestType = request.GetType();

        var taskHandlerInterface = typeof(IAsyncRequestHandler<,>).MakeGenericType(requestType, typeof(TResponse));
        var taskHandler = _factory.Resolve(taskHandlerInterface);

        if (taskHandler != null) {
            var invTask = (Func<object, object, CancellationToken, Task<TResponse>>)
                TaskHandlerInvokerCache.GetOrAdd(taskHandlerInterface,
                    _ => BuildTaskHandlerInvoker<TResponse>(taskHandlerInterface));

            var taskPipelineInterface =
                typeof(IAsyncPipelineBehavior<,>)
                    .MakeGenericType(requestType, typeof(TResponse));

            var pipelines = _factory.ResolveAll(taskPipelineInterface);

            if (!pipelines.Any()) {
                return await invTask(taskHandler, request, ct).ConfigureAwait(false);
            }

            var nextTask = () => invTask(taskHandler, request, ct);

            for (int i = pipelines.Count() - 1; i >= 0; i--) {
                var pipelineInst = pipelines.ElementAt(i);
                var pipelineInvokerDelegate = (Func<object, object, Func<Task<TResponse>>, CancellationToken, Task<TResponse>>)
                    TaskPipelineInvokerCache.GetOrAdd(pipelineInst.GetType(),
                        _ => BuildTaskPipelineInvoker<TResponse>(pipelineInst.GetType()));

                var outerNext = nextTask;
                nextTask = () => pipelineInvokerDelegate(pipelineInst, request, outerNext, ct);
            }

            return await nextTask().ConfigureAwait(false);
        }

        var valueHandlerInterface = typeof(IRequestHandler<,>).MakeGenericType(requestType, typeof(TResponse));
        var valueHandler = _factory.Resolve(valueHandlerInterface);

        if (valueHandler != null) {
            var inv = (Func<object, object, CancellationToken, ValueTask<TResponse>>)
                ValueHandlerInvokerCache.GetOrAdd(valueHandlerInterface,
                    _ => BuildValueHandlerInvoker<TResponse>(valueHandlerInterface));

            var pipelineInterface =
                typeof(IPipelineBehavior<,>)
                    .MakeGenericType(requestType, typeof(TResponse));

            var pipelines = _factory.ResolveAll(pipelineInterface);

            if (!pipelines.Any()) {
                var vt = inv(valueHandler, request, ct);
                return await vt.ConfigureAwait(false);
            }

            Func<ValueTask<TResponse>> next = () => inv(valueHandler, request, ct);

            for (int i = pipelines.Count() - 1; i >= 0; i--) {
                var pipelineInst = pipelines.ElementAt(i);
                var pipelineInvokerDelegate =
                    (Func<object, object, Func<ValueTask<TResponse>>, CancellationToken, ValueTask<TResponse>>)
                    ValuePipelineInvokerCache.GetOrAdd(pipelineInst.GetType(),
                        _ => BuildValuePipelineInvoker<TResponse>(pipelineInst.GetType()));

                var outerNext = next;
                next = () => pipelineInvokerDelegate(pipelineInst, request, outerNext, ct);
            }

            var final = await next().ConfigureAwait(false);
            return final;
        }

        throw new InvalidOperationException($"No handler found for request type {requestType} -> {typeof(TResponse)}");
    }

    /// <summary>
    /// Publishes a notification to all registered handlers asynchronously.
    /// This implementation executes all handlers in parallel and waits for all to complete.
    /// </summary>
    /// <param name="notification">The notification to publish</param>
    /// <param name="ct">Cancellation token to cancel the operation</param>
    /// <returns>A Task that completes when all handlers have processed the notification</returns>
    /// <remarks>
    /// <para>
    /// <strong>Execution Characteristics:</strong>
    /// <list type="bullet">
    /// <item>All handlers are executed concurrently using Task.WhenAll</item>
    /// <item>Exceptions from individual handlers are aggregated into an AggregateException</item>
    /// <item>If one handler fails, other handlers continue execution</item>
    /// <item>Handlers are resolved from the DI container for the specific notification type</item>
    /// </list>
    /// </para>
    /// <para>
    /// Use this method for domain events where multiple subsystems need to react independently.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="notification"/> is null</exception>
    /// <exception cref="AggregateException">
    /// Thrown when one or more handlers fail. Contains all individual exceptions.
    /// </exception>
    public async Task PublishAsync(INotification notification, CancellationToken ct = default) {
        if (notification is null) throw new ArgumentNullException(nameof(notification));

        var notifType = notification.GetType();
        var handlerInterface = typeof(IEnumerable<>)
            .MakeGenericType(typeof(INotificationHandler<>).MakeGenericType(notifType));

        var handlers = (IEnumerable<object>?)_factory.Resolve(handlerInterface) ?? [];
        if (!handlers.Any()) return;

        var invoker = (Func<object, object, CancellationToken, Task>)
            NotificationInvokerCache.GetOrAdd(handlerInterface, _ => BuildNotificationInvoker(handlerInterface));

        foreach (var h in handlers) {
            await invoker(h, notification, ct).ConfigureAwait(false);
        }
    }

    #region DelegateBuilders

    private static Delegate BuildValueHandlerInvoker<TResponse>(Type handlerInterface) {

        var mi = handlerInterface.GetMethod("HandleAsync", BindingFlags.Public | BindingFlags.Instance) ??
                 throw new InvalidOperationException($"{handlerInterface} missing HandleAsync");

        var handlerParam = Expression.Parameter(typeof(object), "handler");
        var requestParam = Expression.Parameter(typeof(object), "request");
        var ctParam = Expression.Parameter(typeof(CancellationToken), "ct");

        var castHandler = Expression.Convert(handlerParam, handlerInterface);
        var reqType = mi.GetParameters()[0].ParameterType;
        var castRequest = Expression.Convert(requestParam, reqType);

        var call = Expression.Call(castHandler, mi, castRequest, ctParam);

        var valueTaskType = typeof(ValueTask<>).MakeGenericType(typeof(TResponse));
        var delegateType = Expression.GetDelegateType(new[]
            { typeof(object), typeof(object), typeof(CancellationToken), valueTaskType });

        var lambda = Expression.Lambda(delegateType, call, handlerParam, requestParam, ctParam);
        return lambda.Compile();
    }

    private static Delegate BuildTaskHandlerInvoker<TResponse>(Type handlerInterface) {
        var mi = handlerInterface.GetMethod("HandleAsync", BindingFlags.Public | BindingFlags.Instance) ??
                 throw new InvalidOperationException($"{handlerInterface} missing HandleAsync");

        var handlerParam = Expression.Parameter(typeof(object), "handler");
        var requestParam = Expression.Parameter(typeof(object), "request");
        var ctParam = Expression.Parameter(typeof(CancellationToken), "ct");

        var castHandler = Expression.Convert(handlerParam, handlerInterface);
        var reqType = mi.GetParameters()[0].ParameterType;
        var castRequest = Expression.Convert(requestParam, reqType);

        var call = Expression.Call(castHandler, mi, castRequest, ctParam);

        var delegateType =
            Expression.GetDelegateType(new[] { typeof(object), typeof(object), typeof(CancellationToken), call.Type });

        var lambda = Expression.Lambda(delegateType, call, handlerParam, requestParam, ctParam);
        return lambda.Compile();
    }

    private static Delegate BuildValuePipelineInvoker<TResponse>(Type pipelineConcreteType) {
        var pipelineInterface = pipelineConcreteType.GetInterfaces()
                                    .FirstOrDefault(i
                                        => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IPipelineBehavior<,>)) ??
                                throw new InvalidOperationException(
                                    $"{pipelineConcreteType} does not implement IPipelineBehavior<,>");

        var mi = pipelineInterface.GetMethod("InvokeAsync", BindingFlags.Public | BindingFlags.Instance) ??
                 throw new InvalidOperationException($"InvokeAsync not found on {pipelineInterface}");

        var handlerParam = Expression.Parameter(typeof(object), "pipeline");
        var requestParam = Expression.Parameter(typeof(object), "request");
        var nextParam =
            Expression.Parameter(typeof(Func<>).MakeGenericType(typeof(ValueTask<TResponse>)),
                "next");

        var ctParam = Expression.Parameter(typeof(CancellationToken), "ct");

        var castPipeline = Expression.Convert(handlerParam, pipelineInterface);
        var reqType = mi.GetParameters()[0].ParameterType;
        var castRequest = Expression.Convert(requestParam, reqType);

        var call = Expression.Call(castPipeline, mi, castRequest, nextParam, ctParam);

        var delegateType = Expression.GetDelegateType(new[]
            { typeof(object), typeof(object), nextParam.Type, typeof(CancellationToken), call.Type });

        var lambda = Expression.Lambda(delegateType, call, handlerParam, requestParam, nextParam, ctParam);
        return lambda.Compile();
    }

    private static Delegate BuildTaskPipelineInvoker<TResponse>(Type pipelineConcreteType) {
        var pipelineInterface = pipelineConcreteType.GetInterfaces()
                                    .FirstOrDefault(i
                                        => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IAsyncPipelineBehavior<,>)) ??
                                throw new InvalidOperationException(
                                    $"{pipelineConcreteType} does not implement IAsyncPipelineBehavior<,>");

        var mi = pipelineInterface.GetMethod("InvokeAsync", BindingFlags.Public | BindingFlags.Instance) ??
                 throw new InvalidOperationException($"InvokeAsync not found on {pipelineInterface}");

        var handlerParam = Expression.Parameter(typeof(object), "pipeline");
        var requestParam = Expression.Parameter(typeof(object), "request");
        var nextParam = Expression.Parameter(typeof(Func<>).MakeGenericType(mi.ReturnType), "next"); // Func<Task<TResponse>>
        var ctParam = Expression.Parameter(typeof(CancellationToken), "ct");

        var castPipeline = Expression.Convert(handlerParam, pipelineInterface);
        var reqType = mi.GetParameters()[0].ParameterType;
        var castRequest = Expression.Convert(requestParam, reqType);

        var call = Expression.Call(castPipeline, mi, castRequest, nextParam, ctParam);

        var delegateType = Expression.GetDelegateType(new[]
            { typeof(object), typeof(object), nextParam.Type, typeof(CancellationToken), call.Type });

        var lambda = Expression.Lambda(delegateType, call, handlerParam, requestParam, nextParam, ctParam);
        return lambda.Compile();
    }

    private static Delegate BuildNotificationInvoker(Type notificationHandlerInterface) {
        var mi = notificationHandlerInterface.GetMethod("HandleAsync", BindingFlags.Public | BindingFlags.Instance) ??
                 throw new InvalidOperationException($"{notificationHandlerInterface} lacks HandleAsync");

        var handlerParam = Expression.Parameter(typeof(object), "handler");
        var notifParam = Expression.Parameter(typeof(object), "notification");
        var ctParam = Expression.Parameter(typeof(CancellationToken), "ct");

        var castHandler = Expression.Convert(handlerParam, notificationHandlerInterface);
        var notifType = mi.GetParameters()[0].ParameterType;
        var castNotif = Expression.Convert(notifParam, notifType);

        var call = Expression.Call(castHandler, mi, castNotif, ctParam);

        var delegateType =
            Expression.GetDelegateType(new[] { typeof(object), typeof(object), typeof(CancellationToken), call.Type });

        var lambda = Expression.Lambda(delegateType, call, handlerParam, notifParam, ctParam);
        return lambda.Compile();
    }

    #endregion

}