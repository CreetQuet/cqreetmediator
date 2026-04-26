using BenchmarkDotNet.Attributes;
using CQReetMediator.Abstractions;
using CQReetMediator.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;

namespace CQReetMediator.Benchmarks;

[MemoryDiagnoser]
[RankColumn]
public class MediatorBenchmarks
{
    private IMediator _cqreetMediator = null!;
    private MediatR.IMediator _mediatr = null!;
    private Ping _request = null!;
    private MediatRPing _mediatrRequest = null!;
    private PingHandler _handlerDirecto = null!;

    [GlobalSetup]
    public void Setup()
    {
        var cqreetServices = new ServiceCollection();
        cqreetServices.AddCQReetMediator();
        var cqreetProvider = cqreetServices.BuildServiceProvider();
        _cqreetMediator = cqreetProvider.GetRequiredService<IMediator>();

        var mediatrServices = new ServiceCollection();
        mediatrServices.AddLogging();
        mediatrServices.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<MediatRPing>());
        //mediatrServices.AddTransient(typeof(MediatR.IPipelineBehavior<,>), typeof(MediatRLoggingBehavior<,>));
        var mediatrProvider = mediatrServices.BuildServiceProvider();
        _mediatr = mediatrProvider.GetRequiredService<MediatR.IMediator>();

        _handlerDirecto = new PingHandler();
        _request = new Ping("Benchmark");
        _mediatrRequest = new MediatRPing("Benchmark");
    }

    [Benchmark(Baseline = true)]
    public Task<string?> DirectCall()
    {
        return _handlerDirecto.HandleAsync(_request, CancellationToken.None);
    }

    [Benchmark]
    public Task<string?> CQReetMediator_Send()
    {
        return _cqreetMediator.SendAsync(_request, CancellationToken.None);
    }

    [Benchmark]
    public Task<string> MediatR_Send()
    {
        return _mediatr.Send(_mediatrRequest, CancellationToken.None);
    }
}

// --- CQReetMediator artifacts ---

public record Ping(string Msg) : IRequest<string>;

public class PingHandler : IRequestHandler<Ping, string>
{
    public Task<string?> HandleAsync(Ping request, CancellationToken ct)
        => Task.FromResult<string?>(request.Msg);
}

// --- MediatR artifacts ---

public record MediatRPing(string Msg) : MediatR.IRequest<string>;

public class MediatRPingHandler : MediatR.IRequestHandler<MediatRPing, string>
{
    public Task<string> Handle(MediatRPing request, CancellationToken ct)
        => Task.FromResult(request.Msg);
}
