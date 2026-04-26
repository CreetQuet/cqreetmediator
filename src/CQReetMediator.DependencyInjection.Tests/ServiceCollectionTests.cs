using Microsoft.Extensions.DependencyInjection;
using CQReetMediator.Abstractions;
using Xunit;

namespace CQReetMediator.DependencyInjection.Tests;

public class ServiceCollectionTests {
    [Fact]
    public void AddCQReetMediator_Should_Register_Mediator_Interface() {
        var services = new ServiceCollection();
        services.AddCQReetMediator(typeof(SyncRequest));
        var provider = services.BuildServiceProvider();

        var mediator = provider.GetService<IMediator>();
        Assert.NotNull(mediator);
    }

    [Fact]
    public async Task AddCQReetMediator_Should_Register_And_Resolve_Handler() {
        var services = new ServiceCollection();
        services.AddCQReetMediator(typeof(SyncRequest));
        services.AddSingleton<PipelineSpy>();

        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        var result = await mediator.SendAsync(new SyncRequest("Hi"));

        Assert.Equal("Sync: Hi", result);
    }

    [Fact]
    public async Task AddCQReetMediator_Should_Register_And_Resolve_AsyncHandler() {
        var services = new ServiceCollection();
        services.AddCQReetMediator(typeof(AsyncRequest));
        services.AddSingleton<PipelineSpy>();

        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        var result = await mediator.SendAsync(new AsyncRequest("World"));

        Assert.Equal("Async: World", result);
    }

    [Fact]
    public async Task AddCQReetMediator_Should_Register_And_Resolve_VoidCommand() {
        var services = new ServiceCollection();
        services.AddCQReetMediator(typeof(VoidCommand));

        var spy = new VoidCommandSpy();
        services.AddSingleton(spy);
        services.AddSingleton<PipelineSpy>();
        services.AddSingleton<VoidPipelineSpy>();

        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        await mediator.SendAsync(new VoidCommand("Fire"));

        Assert.Equal("Fire", spy.LastMsg);
    }

    [Fact]
    public async Task AddCQReetMediator_Should_Register_All_NotificationHandlers() {
        var services = new ServiceCollection();
        services.AddCQReetMediator(typeof(TestNotification));

        var spy = new NotificationSpy();
        services.AddSingleton(spy);

        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        await mediator.PublishAsync(new TestNotification("Event"));

        Assert.Equal(2, spy.Calls.Count);
        Assert.Contains("Handler1-Event", spy.Calls);
        Assert.Contains("Handler2-Event", spy.Calls);
    }

    [Fact]
    public async Task AddCQReetMediator_Should_Register_Generic_Pipelines_Correctly() {
        var services = new ServiceCollection();
        services.AddCQReetMediator(typeof(PipelineRequest));

        var spy = new PipelineSpy();
        services.AddSingleton(spy);

        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        await mediator.SendAsync(new PipelineRequest());

        Assert.True(spy.WasCalled, "The generic pipeline didn't execute");
    }

    [Fact]
    public async Task AddCQReetMediator_Should_Register_Void_Generic_Pipelines() {
        var services = new ServiceCollection();
        services.AddCQReetMediator(typeof(VoidPipelineCommand));

        var spy = new VoidPipelineSpy();
        services.AddSingleton(spy);

        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        await mediator.SendAsync(new VoidPipelineCommand());

        Assert.True(spy.WasCalled, "The void pipeline didn't execute");
    }

    [Fact]
    public void Should_Throw_When_Request_Has_No_Handler() {
        var services = new ServiceCollection();
        services.AddCQReetMediator(typeof(SyncRequest));
        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        var requestWithout = new RequestWithoutHandler();

        Assert.ThrowsAny<Exception>(() => {
            mediator.SendAsync(requestWithout).GetAwaiter().GetResult();
        });
    }

    public record RequestWithoutHandler : IRequest<bool>;
}
