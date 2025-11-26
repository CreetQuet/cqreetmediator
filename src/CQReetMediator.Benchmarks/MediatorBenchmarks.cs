using BenchmarkDotNet.Attributes;
using CQReetMediator.Abstractions;
using CQReetMediator.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace CQReetMediator.Benchmarks;

[MemoryDiagnoser]
[ShortRunJob]
public class MediatorBenchmarks {
    private IMediator _mediator = null!;
    private Ping _request = null!;
    private PingHandler _handlerDirecto = null!;

    [GlobalSetup]
    public void Setup() {

        var services = new ServiceCollection();
        services.AddCQReetMediator(typeof(Ping));
        var provider = services.BuildServiceProvider();

        _mediator = provider.GetRequiredService<IMediator>();


        _handlerDirecto = new PingHandler();
        _request = new Ping("Benchmark");
    }


    [Benchmark(Baseline = true)]
    public async ValueTask<string?> DirectCall() {
        return await _handlerDirecto.HandleAsync(_request, CancellationToken.None);
    }


    [Benchmark]
    public async ValueTask<string?> MediatorSend() {
        return await _mediator.Send(_request, CancellationToken.None);
    }
}

public record Ping(string Msg) : IRequest<string>;

public class PingHandler : IRequestHandler<Ping, string> {
    public ValueTask<string?> HandleAsync(Ping request, CancellationToken ct) {

        return new ValueTask<string?>(request.Msg);
    }
}