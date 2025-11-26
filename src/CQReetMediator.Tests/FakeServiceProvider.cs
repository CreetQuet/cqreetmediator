namespace CQReetMediator.Tests;

public class FakeServiceProvider : IServiceProvider {
    private readonly Dictionary<Type, object> _services = new();

    public void Register(Type type, object instance) => _services[type] = instance;

    public object? GetService(Type serviceType) {
        if (_services.TryGetValue(serviceType, out var instance)) return instance;
        return null;
    }
}