using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using CQReetMediator.Abstractions;

namespace CQReetMediator.Benchmarks;

[SimpleJob(RuntimeMoniker.Net90, launchCount: 1, warmupCount: 3)]
[MemoryDiagnoser]
[ThreadingDiagnoser]
public class MediatorMassiveLoadBenchmarks {
    private readonly IMediator _mediator;

    public MediatorMassiveLoadBenchmarks() {
        _mediator = MediatorFactory.Create();
    }

    [Params(10_000, 50_000, 100_000)] public int Count;

    // --------------- MASSIVE SEND -------------------------

    [Benchmark]
    public async Task Massive_Send_NoPipelines() {
        for (int i = 0; i < Count; i++)
            await _mediator.SendAsync(new TestRequest(i));
    }

    [Benchmark]
    public async Task Massive_Send_WithPipelines() {
        for (int i = 0; i < Count; i++)
            await _mediator.SendAsync(new PipelineRequest(i));
    }

    // --------------- MASSIVE PUBLISH ----------------------

    [Benchmark]
    public async Task Massive_Publish_3Handlers() {
        for (int i = 0; i < Count; i++)
            await _mediator.PublishAsync(new TestNotification());
    }

    // --------------- PARALLEL SEND ------------------------

    [Benchmark]
    public async Task Parallel_Send_100Tasks() {
        var range = Enumerable.Range(0, 100);

        await Parallel.ForEachAsync(range, async (_, _) => {
            for (int i = 0; i < 10_000; i++)
                await _mediator.SendAsync(new TestRequest(i));
        });
    }


}