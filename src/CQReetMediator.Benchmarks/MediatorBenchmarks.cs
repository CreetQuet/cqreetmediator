using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using CQReetMediator.Abstractions;

namespace CQReetMediator.Benchmarks;

[MemoryDiagnoser]
[RankColumn]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
public class MediatorBenchmarks {
    private IMediator? _mediatorNoPipelines;
    private IMediator? _mediatorWithPipelines;
    private IMediator? _mediatorNotifications;

    private readonly TestRequest _request = new(1);
    private readonly TestNotification _notif = new();

    [GlobalSetup]
    public void Setup() {
        _mediatorNoPipelines = new Mediator(
            singleResolver: type => {
                if (type == typeof(IRequestHandler<TestRequest, int>))
                    return new TestRequestHandler();

                return null;
            },
            multipleResolver: type => Array.Empty<object>()
        );

        _mediatorWithPipelines = new Mediator(
            singleResolver: type => {
                if (type == typeof(IRequestHandler<TestRequest, int>))
                    return new TestRequestHandler();

                return null;
            },
            multipleResolver: type => {
                if (type == typeof(IPipelineBehavior<TestRequest, int>))
                    return new object[] {
                        new TestPipeline1(),
                        new TestPipeline2()
                    };

                return Array.Empty<object>();
            }
        );

        _mediatorNotifications = new Mediator(
            singleResolver: type => null,
            multipleResolver: type => {
                if (type == typeof(INotificationHandler<TestNotification>))
                    return new object[] {
                        new TestNotificationHandler(),
                        new TestNotificationHandler(),
                        new TestNotificationHandler(),
                    };

                return Array.Empty<object>();
            }
        );
    }

    [Benchmark(Baseline = true)]
    public Task<int> Send_NoPipelines() =>
        _mediatorNoPipelines!.SendAsync(_request);

    [Benchmark]
    public Task<int> Send_WithPipelines() =>
        _mediatorWithPipelines!.SendAsync(_request);

    [Benchmark]
    public Task Publish_ThreeHandlers() =>
        _mediatorNotifications!.PublishAsync(_notif);
}