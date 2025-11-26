using CQReetMediator.Abstractions;

namespace CQReetMediator.DependencyInjection.Tests;

public record SyncRequest(string Msg) : IRequest<string>;

public class SyncHandler : IRequestHandler<SyncRequest, string> {
    public ValueTask<string> HandleAsync(SyncRequest request, CancellationToken ct) 
        => new($"Sync: {request.Msg}");
}

public record AsyncRequest(string Msg) : IRequest<string>;

public class AsyncHandler : IAsyncRequestHandler<AsyncRequest, string> {
    public Task<string> HandleAsync(AsyncRequest request, CancellationToken ct) 
        => Task.FromResult($"Async: {request.Msg}");
}

public record TestNotification(string Msg) : INotification;

public class NotificationSpy {
    public List<string> Calls { get; } = new();
}

public class NotifHandler1(NotificationSpy spy) : INotificationHandler<TestNotification> {
    public Task HandleAsync(TestNotification notification, CancellationToken ct) {
        spy.Calls.Add($"Handler1-{notification.Msg}");
        return Task.CompletedTask;
    }
}

public class NotifHandler2(NotificationSpy spy) : INotificationHandler<TestNotification> {
    public Task HandleAsync(TestNotification notification, CancellationToken ct) {
        spy.Calls.Add($"Handler2-{notification.Msg}");
        return Task.CompletedTask;
    }
}

public record PipelineRequest : IRequest<bool>;

public class PipelineRequestDummyHandler : IRequestHandler<PipelineRequest, bool> {
    public ValueTask<bool> HandleAsync(PipelineRequest request, CancellationToken ct) => new(true);
}

public class PipelineSpy {
    public bool WasCalled { get; set; }
}

public class TestSpyPipeline<TRequest, TResponse>(PipelineSpy spy) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse> {
    public async ValueTask<TResponse> InvokeAsync(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct) {
        spy.WasCalled = true;
        return await next();
    }
}

public class TestSpyPipelineAsync<TRequest, TResponse>(PipelineSpy spy) : IAsyncPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse> {
    public async Task<TResponse> InvokeAsync(TRequest request, RequestHandlerDelegateAsync<TResponse> next, CancellationToken ct) {
        spy.WasCalled = true;
        return await next();
    }
}