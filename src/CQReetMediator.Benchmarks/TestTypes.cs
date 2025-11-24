// using CQReetMediator.Abstractions;
//
// public sealed class TestRequest : IRequest<int> { }
//
// public sealed class TestRequestHandler : IRequestHandler<TestRequest, int> {
//     public async ValueTask<int> HandleAsync(TestRequest request, CancellationToken ct)
//         => 42;
// }
//
// public sealed class TestPipeline1 : IPipelineBehavior<TestRequest, int> {
//     public async ValueTask<int> InvokeAsync(TestRequest request, Func<ValueTask<int>> next, CancellationToken ct)
//         => await next();
// }
//
// public sealed class TestPipeline2 : IPipelineBehavior<TestRequest, int> {
//     public async ValueTask<int> InvokeAsync(TestRequest request, Func<ValueTask<int>> next, CancellationToken ct)
//         => (await next()) + 1;
// }
//
// public sealed class TestNotification : INotification { }
//
// public sealed class TestNotificationHandler : INotificationHandler<TestNotification> {
//     public Task HandleAsync(TestNotification notification, CancellationToken ct)
//         => Task.CompletedTask;
// }