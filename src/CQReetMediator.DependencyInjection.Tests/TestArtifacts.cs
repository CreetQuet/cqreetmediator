using CQReetMediator.Abstractions;

namespace CQReetMediator.DependencyInjection.Tests;

public record SyncRequest(string Msg) : IRequest<string>;

public class SyncHandler : IRequestHandler<SyncRequest, string> {
    public Task<string?> HandleAsync(SyncRequest request, CancellationToken ct)
        => Task.FromResult<string?>($"Sync: {request.Msg}");
}

public record AsyncRequest(string Msg) : IRequest<string>;

public class AsyncHandler : IRequestHandler<AsyncRequest, string> {
    public async Task<string?> HandleAsync(AsyncRequest request, CancellationToken ct) {
        await Task.Yield();
        return $"Async: {request.Msg}";
    }
}

public record VoidCommand(string Msg) : IRequest;

public class VoidCommandSpy {
    public string? LastMsg { get; set; }
}

public class VoidCommandHandler(VoidCommandSpy spy) : IRequestHandler<VoidCommand> {
    public Task HandleAsync(VoidCommand request, CancellationToken ct) {
        spy.LastMsg = request.Msg;
        return Task.CompletedTask;
    }
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
    public Task<bool> HandleAsync(PipelineRequest request, CancellationToken ct)
        => Task.FromResult(true);
}

public class PipelineSpy {
    public bool WasCalled { get; set; }
}

public class TestSpyPipeline<TRequest, TResponse>(PipelineSpy spy) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse> {
    public async Task<TResponse?> InvokeAsync(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct) {
        spy.WasCalled = true;
        return await next();
    }
}

public record VoidPipelineCommand : IRequest;

public class VoidPipelineSpy {
    public bool WasCalled { get; set; }
}

public class VoidPipelineDummyHandler : IRequestHandler<VoidPipelineCommand> {
    public Task HandleAsync(VoidPipelineCommand request, CancellationToken ct)
        => Task.CompletedTask;
}

public class TestVoidSpyPipeline<TRequest>(VoidPipelineSpy spy) : IPipelineBehavior<TRequest>
    where TRequest : IRequest {
    public async Task InvokeAsync(TRequest request, RequestHandlerDelegate next, CancellationToken ct) {
        spy.WasCalled = true;
        await next();
    }
}
