using CQReetMediator.Abstractions;
using Xunit;

namespace CQReetMediator.Tests;

public class MediatorCoreTests
{
    private MediatorRegistry CreateRegistry(bool includeVoid = false, bool hasPre = false, bool hasPipe = false,
        bool hasPost = false)
    {
        var reqWrappers = new Dictionary<Type, object>();
        var voidWrappers = new Dictionary<Type, object>();
        var notifWrappers = new Dictionary<Type, NotificationWrapperBase>();

        var wrapperType = typeof(RequestWrapper<,>).MakeGenericType(typeof(Ping), typeof(string));
        reqWrappers.Add(typeof(Ping), Activator.CreateInstance(wrapperType, hasPre, hasPipe, hasPost)!);

        if (includeVoid)
        {
            var voidWrapperType = typeof(VoidRequestWrapper<>).MakeGenericType(typeof(VoidPing));
            voidWrappers.Add(typeof(VoidPing), Activator.CreateInstance(voidWrapperType, hasPre, hasPipe, hasPost)!);
        }

        return new MediatorRegistry(reqWrappers, voidWrappers, notifWrappers);
    }

    [Fact]
    public async Task SendAsync_Should_Locate_Wrapper_And_Execute_Handler()
    {
        var container = new FakeServiceProvider();
        container.Register(typeof(IRequestHandler<Ping, string>), new PingHandler());
        container.Register(typeof(IEnumerable<IPipelineBehavior<Ping, string>>),
            new List<IPipelineBehavior<Ping, string>>());

        var registry = CreateRegistry();
        var mediator = new Mediator(container, registry);

        var response = await mediator.SendAsync(new Ping("Manual"));

        Assert.Equal("Pong: Manual", response);
    }

    [Fact]
    public async Task SendAsync_Should_Execute_Pipelines_Manually_Injected()
    {
        var spy = new PipelineSpy();
        var container = new FakeServiceProvider();

        container.Register(typeof(IRequestHandler<Ping, string>), new PingHandler());

        var pipelineInstance = new TestPipeline<Ping, string>(spy);
        var pipelines = new List<IPipelineBehavior<Ping, string>> { pipelineInstance };
        container.Register(typeof(IEnumerable<IPipelineBehavior<Ping, string>>), pipelines);

        var registry = CreateRegistry(hasPipe: true);
        var mediator = new Mediator(container, registry);

        await mediator.SendAsync(new Ping("With Pipeline"));

        Assert.True(spy.Executed, "The pipeline should have been executed");
    }

    [Fact]
    public async Task SendAsync_Should_Execute_Pre_And_Post_Processors()
    {
        var preSpy = new PipelineSpy();
        var postSpy = new PipelineSpy();
        var container = new FakeServiceProvider();

        container.Register(typeof(IRequestHandler<Ping, string>), new PingHandler());

        var preInstance = new TestPreProcessor<Ping, string>(preSpy);
        var pres = new List<IPreProcessorBehavior<Ping, string>> { preInstance };
        container.Register(typeof(IEnumerable<IPreProcessorBehavior<Ping, string>>), pres);

        var postInstance = new TestPostProcessor<Ping, string>(postSpy);
        var posts = new List<IPostProcessorBehavior<Ping, string>> { postInstance };
        container.Register(typeof(IEnumerable<IPostProcessorBehavior<Ping, string>>), posts);

        // 4. Encendemos Pre y Post solo para este test
        var registry = CreateRegistry(hasPre: true, hasPost: true);
        var mediator = new Mediator(container, registry);

        await mediator.SendAsync(new Ping("With Processors"));

        Assert.True(preSpy.Executed, "The pre-processor should have been executed");
        Assert.True(postSpy.Executed, "The post-processor should have been executed");
    }

    [Fact]
    public async Task SendAsync_Should_Throw_If_Handler_Not_Registered_In_Container()
    {
        var container = new FakeServiceProvider();

        var registry = CreateRegistry();
        var mediator = new Mediator(container, registry);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await mediator.SendAsync(new Ping("Fail"));
        });

        Assert.Contains("Handler not found", exception.Message);
    }

    [Fact]
    public async Task SendAsync_Void_Should_Execute_Handler()
    {
        var handler = new VoidPingHandler();
        var container = new FakeServiceProvider();
        container.Register(typeof(IRequestHandler<VoidPing>), handler);
        container.Register(typeof(IEnumerable<IPipelineBehavior<VoidPing>>), new List<IPipelineBehavior<VoidPing>>());

        var registry = CreateRegistry(includeVoid: true);
        var mediator = new Mediator(container, registry);

        await mediator.SendAsync(new VoidPing("Hello"));

        Assert.Equal("Hello", handler.LastMsg);
    }
}