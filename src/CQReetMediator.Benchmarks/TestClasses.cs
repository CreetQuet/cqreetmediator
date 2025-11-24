using CQReetMediator.Abstractions;

namespace CQReetMediator.Benchmarks;

public record TestRequest(int Value) : IRequest<int>;

public record PipelineRequest(int Value) : IRequest<int>;

public record TestNotification : INotification;

public class TestRequestHandler : IRequestHandler<TestRequest, int> {
    public ValueTask<int> HandleAsync(TestRequest request, CancellationToken ct)
        => ValueTask.FromResult(request.Value);
}

public class PipelineRequestHandler : IRequestHandler<PipelineRequest, int> {
    public ValueTask<int> HandleAsync(PipelineRequest request, CancellationToken ct)
        => ValueTask.FromResult(request.Value);
}

public class FakePipeline<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse> where TRequest : IRequest<TResponse> {
    public async ValueTask<TResponse> InvokeAsync(TRequest request, Func<ValueTask<TResponse>> next, CancellationToken ct)
        => await next();
}

public class TestNotificationHandler1 : INotificationHandler<TestNotification> {
    public Task HandleAsync(TestNotification notification, CancellationToken ct) => Task.CompletedTask;
}

public class TestNotificationHandler2 : INotificationHandler<TestNotification> {
    public Task HandleAsync(TestNotification notification, CancellationToken ct) => Task.CompletedTask;
}

public class TestNotificationHandler3 : INotificationHandler<TestNotification> {
    public Task HandleAsync(TestNotification notification, CancellationToken ct) => Task.CompletedTask;
}

public sealed class TestPipeline1 : IPipelineBehavior<TestRequest, int> {
    public async ValueTask<int> InvokeAsync(TestRequest request, Func<ValueTask<int>> next, CancellationToken ct)
        => await next();
}

public sealed class TestPipeline2 : IPipelineBehavior<TestRequest, int> {
    public async ValueTask<int> InvokeAsync(TestRequest request, Func<ValueTask<int>> next, CancellationToken ct)
        => (await next()) + 1;
}

public sealed class TestNotificationHandler : INotificationHandler<TestNotification> {
    public Task HandleAsync(TestNotification notification, CancellationToken ct)
        => Task.CompletedTask;
}