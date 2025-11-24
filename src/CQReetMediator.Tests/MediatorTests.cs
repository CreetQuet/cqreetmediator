using CQReetMediator.Abstractions;
using Xunit;

namespace CQReetMediator.Tests;

public class MediatorTests {
    // ---------------------------
    // Helpers (Mock Handlers)
    // ---------------------------

    public class TestCommand : ICommand<int> {
        public int Value { get; set; }
    }

    public class TestCommandHandler : IRequestHandler<TestCommand, int> {
        public ValueTask<int> HandleAsync(TestCommand cmd, CancellationToken ct)
            => ValueTask.FromResult(cmd.Value * 2);
    }

    public class TestPipeline : IPipelineBehavior<TestCommand, int> {
        public List<string> Calls = new();

        public async ValueTask<int> InvokeAsync(TestCommand request, Func<ValueTask<int>> next, CancellationToken ct) {
            Calls.Add("before");
            var result = await next();
            Calls.Add("after");
            return result + 1;
        }
    }

    public class TestNotification : INotification { }

    public class TestNotificationHandlerA : INotificationHandler<TestNotification> {
        public bool Called = false;

        public Task HandleAsync(TestNotification notification, CancellationToken ct) {
            Called = true;
            return Task.CompletedTask;
        }
    }

    public class TestNotificationHandlerB : INotificationHandler<TestNotification> {
        public bool Called = false;

        public Task HandleAsync(TestNotification notification, CancellationToken ct) {
            Called = true;
            return Task.CompletedTask;
        }
    }

    // ---------------------------
    // Tests
    // ---------------------------

    [Fact]
    public async Task SendAsync_Should_Invoke_Handler() {
        var handler = new TestCommandHandler();

        var mediator = new Mediator(
            singleResolver: t => t == typeof(IRequestHandler<TestCommand, int>)
                ? handler
                : null,
            multipleResolver: t => Array.Empty<object>()
        );

        var res = await mediator.SendAsync(new TestCommand { Value = 3 });

        Assert.Equal(6, res);
    }

    [Fact]
    public async Task SendAsync_Should_Invoke_Pipelines_In_Order() {
        var handler = new TestCommandHandler();
        var pipeline = new TestPipeline();

        var mediator = new Mediator(
            singleResolver: t => t == typeof(IRequestHandler<TestCommand, int>)
                ? handler
                : null,
            multipleResolver: t => t == typeof(IPipelineBehavior<TestCommand, int>)
                ? new[] { pipeline }
                : Array.Empty<object>()
        );

        var res = await mediator.SendAsync(new TestCommand { Value = 5 });

        Assert.Equal(11, res);
        Assert.Equal(new[] { "before", "after" }, pipeline.Calls);
    }

    [Fact]
    public async Task SendAsync_Should_Throw_When_No_Handler() {
        var mediator = new Mediator(
            singleResolver: t => null,
            multipleResolver: t => Array.Empty<object>()
        );

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            mediator.SendAsync(new TestCommand { Value = 1 }));
    }

    [Fact]
    public async Task PublishAsync_Should_Call_All_Notification_Handlers() {
        var handlerA = new TestNotificationHandlerA();
        var handlerB = new TestNotificationHandlerB();

        var mediator = new Mediator(
            singleResolver: _ => null,
            multipleResolver: t =>
                t == typeof(INotificationHandler<TestNotification>)
                    ? new object[] { handlerA, handlerB }
                    : Array.Empty<object>()
        );

        await mediator.PublishAsync(new TestNotification());

        Assert.True(handlerA.Called);
        Assert.True(handlerB.Called);
    }

    [Fact]
    public async Task PublishAsync_Should_Invoke_Handlers_In_Sequence() {
        List<string> order = new();

        var mediator = new Mediator(
            singleResolver: _ => null,
            multipleResolver: t =>
                t == typeof(INotificationHandler<TestNotification>)
                    ? new object[] {
                        new InlineNotificationHandler(_ => order.Add("A")),
                        new InlineNotificationHandler(_ => order.Add("B"))
                    }
                    : Array.Empty<object>()
        );

        await mediator.PublishAsync(new TestNotification());

        Assert.Equal(new[] { "A", "B" }, order);
    }

    public class InlineNotificationHandler : INotificationHandler<TestNotification> {
        private readonly Action<TestNotification> _act;

        public InlineNotificationHandler(Action<TestNotification> act) => _act = act;

        public Task HandleAsync(TestNotification notification, CancellationToken ct) {
            _act(notification);
            return Task.CompletedTask;
        }
    }

}