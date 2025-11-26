using CQReetMediator.Abstractions;
using Xunit;

namespace CQReetMediator.Tests;

public class MediatorCoreTests {
    private MediatorRegistry CreateManualRegistry() {
        var reqWrappers = new Dictionary<Type, object>();
        var asyncWrappers = new Dictionary<Type, object>();
        var notifWrappers = new Dictionary<Type, NotificationWrapperBase>();

        var wrapperType = typeof(RequestWrapper<,>).MakeGenericType(typeof(Ping), typeof(string));
        var wrapperInstance = Activator.CreateInstance(wrapperType)!;

        reqWrappers.Add(typeof(Ping), wrapperInstance);

        return new MediatorRegistry(reqWrappers, asyncWrappers, notifWrappers);
    }

    [Fact]
    public async Task Send_Should_Locate_Wrapper_And_Execute_Handler() {
        var container = new FakeServiceProvider();
        container.Register(typeof(IRequestHandler<Ping, string>), new PingHandler());
        container.Register(typeof(IEnumerable<IPipelineBehavior<Ping, string>>), new List<IPipelineBehavior<Ping, string>>());

        var registry = CreateManualRegistry();
        var mediator = new Mediator(container, registry);

        var response = await mediator.Send(new Ping("Manual"));

        Assert.Equal("Pong: Manual", response);
    }

    [Fact]
    public async Task Send_Should_Execute_Pipelines_Manually_Injected() {
        var spy = new PipelineSpy();
        var container = new FakeServiceProvider();

        container.Register(typeof(IRequestHandler<Ping, string>), new PingHandler());

        var pipelineInstance = new TestPipeline<Ping, string>(spy);
        var pipelines = new List<IPipelineBehavior<Ping, string>> { pipelineInstance };
        container.Register(typeof(IEnumerable<IPipelineBehavior<Ping, string>>), pipelines);

        var registry = CreateManualRegistry();
        var mediator = new Mediator(container, registry);

        await mediator.Send(new Ping("With Pipeline"));

        Assert.True(spy.Executed, "The pipeline should have been executed");
    }

    [Fact]
    public async Task Send_Should_Throw_If_Handler_Not_Registered_In_Container() {
        var container = new FakeServiceProvider();

        var registry = CreateManualRegistry();
        var mediator = new Mediator(container, registry);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () => { await mediator.Send(new Ping("Fail")); });

        Assert.Contains("Handler not found", exception.Message);
    }
}