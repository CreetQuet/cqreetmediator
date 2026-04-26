using CQReetMediator.Abstractions;

namespace CQReetMediator.Tests;

public record Ping(string Msg) : IRequest<string>;

public class PingHandler : IRequestHandler<Ping, string> {
    public Task<string?> HandleAsync(Ping request, CancellationToken ct)
        => Task.FromResult<string?>($"Pong: {request.Msg}");
}

public record VoidPing(string Msg) : IRequest;

public class VoidPingHandler : IRequestHandler<VoidPing> {
    public string? LastMsg { get; private set; }

    public Task HandleAsync(VoidPing request, CancellationToken ct) {
        LastMsg = request.Msg;
        return Task.CompletedTask;
    }
}

public class PipelineSpy {
    public bool Executed { get; set; }
}

public class TestPipeline<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse> {
    private readonly PipelineSpy _spy;
    public TestPipeline(PipelineSpy spy) => _spy = spy;

    public async Task<TResponse?> InvokeAsync(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct) {
        _spy.Executed = true;
        return await next();
    }
}

public class TestPreProcessor<TRequest, TResponse> : IPreProcessorBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse> {
    private readonly PipelineSpy _spy;
    public TestPreProcessor(PipelineSpy spy) => _spy = spy;

    public Task ProcessAsync(TRequest request, CancellationToken ct) {
        _spy.Executed = true;
        return Task.CompletedTask;
    }
}

public class TestPostProcessor<TRequest, TResponse> : IPostProcessorBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse> {
    private readonly PipelineSpy _spy;
    public TestPostProcessor(PipelineSpy spy) => _spy = spy;

    public Task ProcessAsync(TRequest request, TResponse? response, CancellationToken ct) {
        _spy.Executed = true;
        return Task.CompletedTask;
    }
}
