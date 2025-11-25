namespace CQReetMediator.Abstractions;

public interface IServiceFactory {
    /// <summary>
    /// Resolves a single service instance for the specified service type.
    /// Returns <c>null</c> if the service is not registered.
    /// </summary>
    /// <param name="serviceType">The type of the service to resolve.</param>
    /// <returns>
    /// An instance of the requested service type, or <c>null</c> if not found.
    /// </returns>
    object? Resolve(Type serviceType);

    /// <summary>
    /// Resolves all registered service instances for the specified service type.
    /// </summary>
    /// <param name="serviceType">The type of the service to resolve.</param>
    /// <returns>
    /// A sequence containing all registered instances of the specified service type.
    /// </returns>
    IEnumerable<object> ResolveAll(Type serviceType);
}