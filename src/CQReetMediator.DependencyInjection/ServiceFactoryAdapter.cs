using CQReetMediator.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace CQReetMediator.DependencyInjection;

/// <summary>
/// Adapter that bridges the CQReetMediator <see cref="IServiceFactory"/> abstraction
/// with the underlying Microsoft.Extensions.DependencyInjection container.
/// </summary>
/// <remarks>
/// This implementation allows the mediator to operate independently of any specific
/// DI container by delegating service resolution to <see cref="IServiceProvider"/>.
/// </remarks>
public sealed class ServiceFactoryAdapter : IServiceFactory {
    private readonly IServiceProvider _provider;

    /// <summary>
    /// Initializes a new instance of <see cref="ServiceFactoryAdapter"/>.
    /// </summary>
    /// <param name="provider">The underlying <see cref="IServiceProvider"/> to use for resolution.</param>
    public ServiceFactoryAdapter(IServiceProvider provider)
        => _provider = provider;

    /// <inheritdoc />
    public object? Resolve(Type serviceType)
        => _provider.GetService(serviceType) as object;

    /// <inheritdoc />
    public IEnumerable<object> ResolveAll(Type serviceType)
        => _provider.GetServices(serviceType).Cast<object>();
}