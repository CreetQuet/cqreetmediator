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
    public async Task AddCQReetMediator_Should_Register_And_Resolve_SyncHandler() {
        var services = new ServiceCollection();

        services.AddCQReetMediator(typeof(SyncRequest));
        services.AddSingleton<PipelineSpy>();

        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        var result = await mediator.Send(new SyncRequest("Hi"));

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

        await mediator.Send(new PipelineRequest());

        Assert.True(spy.WasCalled, "The generic pipeline didn't execute");
    }

    [Fact]
    public void Should_Throw_When_Request_Has_No_Handler() {
        var services = new ServiceCollection();
        services.AddCQReetMediator(typeof(SyncRequest));
        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        var requestWithout = new RequestWithoutHandler();

        Assert.ThrowsAny<Exception>(() => { mediator.Send(requestWithout).AsTask().GetAwaiter().GetResult(); });
    }

    public record RequestWithoutHandler : IRequest<bool>;
}