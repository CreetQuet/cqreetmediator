using CQReetMediator.Abstractions;

namespace CQReetMediator.Tests;

public record Ping(string Msg) : IRequest<string>;

public class PingHandler : IRequestHandler<Ping, string> {
    public ValueTask<string> HandleAsync(Ping request, CancellationToken ct)
        => new($"Pong: {request.Msg}");
}

public class PipelineSpy {
    public bool Executed { get; set; }
}

public class TestPipeline<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse> {
    private readonly PipelineSpy _spy;
    public TestPipeline(PipelineSpy spy) => _spy = spy;

    public async ValueTask<TResponse> InvokeAsync(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct) {
        _spy.Executed = true;
        return await next();
    }
}