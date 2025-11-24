using CQReetMediator.Abstractions;

namespace CQReetMediator.Benchmarks;

public static class MediatorFactory {
    public static IMediator Create() {
        var services = new Dictionary<Type, List<object>>();

        // ---------------------------
        // REGISTER REQUEST HANDLERS
        // ---------------------------
        Register(services, typeof(IRequestHandler<TestRequest, int>), new TestRequestHandler());
        Register(services, typeof(IRequestHandler<PipelineRequest, int>), new PipelineRequestHandler());

        // ---------------------------
        // REGISTER PIPELINES
        // ---------------------------
        Register(services, typeof(IPipelineBehavior<PipelineRequest, int>), new FakePipeline<PipelineRequest, int>());

        // ---------------------------
        // REGISTER NOTIFICATION HANDLERS
        // ---------------------------
        Register(services, typeof(INotificationHandler<TestNotification>), new TestNotificationHandler1());
        Register(services, typeof(INotificationHandler<TestNotification>), new TestNotificationHandler2());
        Register(services, typeof(INotificationHandler<TestNotification>), new TestNotificationHandler3());

        // Factory for single instance
        object? Single(Type t) {
            return services.TryGetValue(t, out var list)
                ? list.FirstOrDefault()
                : null;
        }

        // Factory for multiple instances
        IEnumerable<object> Multiple(Type t) {
            return services.TryGetValue(t, out var list)
                ? list
                : Enumerable.Empty<object>();
        }

        return new Mediator(Single, Multiple);
    }

    private static void Register(Dictionary<Type, List<object>> svc, Type type, object impl) {
        if (!svc.TryGetValue(type, out var list))
            svc[type] = list = new List<object>();

        list.Add(impl);
    }
}