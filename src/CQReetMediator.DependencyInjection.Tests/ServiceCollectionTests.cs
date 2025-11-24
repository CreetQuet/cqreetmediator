using CQReetMediator.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CQReetMediator.DependencyInjection.Tests;

public sealed class ServiceCollectionTests {
    //
    // ──────────────────────────────────────────────
    //  TEST: REGISTERS CORE SERVICES
    // ──────────────────────────────────────────────
    //
    [Fact]
    public void AddCQReetMediator_Should_Register_Core_Services() {
        var services = new ServiceCollection();

        services.AddCQReetMediator(typeof(TestCommand).Assembly);

        var provider = services.BuildServiceProvider();
        Assert.NotNull(provider.GetService<IMediator>());

        Assert.Same(
            provider.GetService<IMediator>(),
            provider.GetService<IMediator>()); // singleton
    }

    [Fact]
    public void AddCQReetMediator_Should_Register_Command_And_Query_Handlers() {
        var services = new ServiceCollection();

        services.AddCQReetMediator(typeof(TestCommand).Assembly);

        var provider = services.BuildServiceProvider();

        var commandHandler = provider.GetService<IRequestHandler<TestCommand, string>>();
        var queryHandler = provider.GetService<IRequestHandler<TestQuery, int>>();

        Assert.NotNull(commandHandler);
        Assert.NotNull(queryHandler);
    }

    [Fact]
    public void AddCQReetMediator_Should_Register_Notification_Handlers() {
        var services = new ServiceCollection();

        services.AddCQReetMediator(typeof(NotificationAHandler).Assembly);

        var provider = services.BuildServiceProvider();

        var handlers = provider.GetService<IEnumerable<INotificationHandler<TestEvent>>>();

        Assert.NotNull(handlers);
        Assert.Equal(2, handlers.Count());
    }

    [Fact]
    public void AddCQReetMediator_Should_Register_Pipeline_Behaviors() {
        var services = new ServiceCollection();

        services.AddCQReetMediator(typeof(PipelineTestCommand).Assembly);

        var provider = services.BuildServiceProvider();

        var behaviors = provider.GetService<IEnumerable<IPipelineBehavior<PipelineTestCommand, bool>>>();

        Assert.NotNull(behaviors);
        Assert.Single(behaviors);
    }

    [Fact]
    public async Task Mediator_Should_Execute_Command_Through_DI() {
        var services = new ServiceCollection();

        services.AddCQReetMediator(typeof(TestCommand).Assembly);

        var provider = services.BuildServiceProvider();

        var mediator = provider.GetRequiredService<IMediator>();

        var result = await mediator.SendAsync(new TestCommand("Hello"));
        Assert.Equal("Handled: Hello", result);
    }

    //
    // ──────────────────────────────────────────────
    //  TEST: END-TO-END NOTIFICATIONS
    // ──────────────────────────────────────────────
    //
    [Fact]
    public async Task Mediator_Should_Invoke_All_Notification_Handlers() {
        var services = new ServiceCollection();

        services.AddCQReetMediator(typeof(NotificationAHandler).Assembly);

        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        var eventInstance = new TestEvent("Ping");

        var handlers = provider.GetRequiredService<IEnumerable<INotificationHandler<TestEvent>>>();

        var a = handlers.OfType<NotificationAHandler>().First();
        var b = handlers.OfType<NotificationBHandler>().First();

        await mediator.PublishAsync(eventInstance);

        Assert.True(a.Called);
        Assert.True(b.Called);
    }

    //
    // ──────────────────────────────────────────────
    //  TEST: PIPELINES EXECUTE DURING DI RESOLUTION
    // ──────────────────────────────────────────────
    //
    [Fact]
    public async Task Mediator_Should_Execute_Pipeline_Behaviors() {
        var services = new ServiceCollection();

        services.AddCQReetMediator(typeof(TestPipeline).Assembly);

        var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        await mediator.SendAsync(new PipelineTestCommand());

        var pipeline = provider.GetRequiredService<IEnumerable<IPipelineBehavior<PipelineTestCommand, bool>>>()
            .OfType<TestPipeline>()
            .First();


        Assert.True(pipeline.Called);
    }

    //
    // ──────────────────────────────────────────────
    //  SUPPORTING TEST TYPES
    // ──────────────────────────────────────────────
    //

    public interface ICommand<TResponse> : IRequest<TResponse> { }

    public interface IQuery<TResponse> : IRequest<TResponse> { }

    public sealed record TestCommand(string Message) : ICommand<string>;

    public sealed class TestCommandHandler : IRequestHandler<TestCommand, string> {
        public ValueTask<string> HandleAsync(TestCommand command, CancellationToken ct)
            => ValueTask.FromResult($"Handled: {command.Message}");
    }

    public sealed record TestQuery(string Query) : IQuery<int>;

    public sealed class TestQueryHandler : IRequestHandler<TestQuery, int> {
        public ValueTask<int> HandleAsync(TestQuery request, CancellationToken ct)
            => ValueTask.FromResult(42);
    }

    public sealed record TestEvent(string Value) : INotification;

    public sealed class NotificationAHandler : INotificationHandler<TestEvent> {
        public bool Called { get; private set; }

        public Task HandleAsync(TestEvent notification, CancellationToken ct) {
            Called = true;
            return Task.CompletedTask;
        }
    }

    public sealed class NotificationBHandler : INotificationHandler<TestEvent> {
        public bool Called { get; private set; }

        public Task HandleAsync(TestEvent notification, CancellationToken ct) {
            Called = true;
            return Task.CompletedTask;
        }
    }

    public sealed record PipelineTestCommand : ICommand<bool>;

    public sealed class PipelineTestHandler : IRequestHandler<PipelineTestCommand, bool> {
        public ValueTask<bool> HandleAsync(PipelineTestCommand cmd, CancellationToken ct)
            => ValueTask.FromResult(true);
    }

    public sealed class TestPipeline : IPipelineBehavior<PipelineTestCommand, bool> {
        public bool Called { get; private set; }

        public ValueTask<bool> InvokeAsync(
            PipelineTestCommand request,
            Func<ValueTask<bool>> next,
            CancellationToken ct) {
            Called = true;
            return next();
        }
    }

}