using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using CQReetMediator.Abstractions;

namespace CQReetMediator.DependencyInjection;

/// <summary>
/// Extension methods for configuring CQReetMediator in the dependency injection container.
/// </summary>
public static class ServiceCollectionExtensions {
    
    /// <summary>
    /// Scans the specified assemblies (or calling assembly) to register all Handlers, Pipelines, and the Mediator itself.
    /// Automatically builds the <see cref="MediatorRegistry"/> for optimized execution.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <param name="assemblyMarkers">Optional types used to mark which assemblies to scan. If empty, scans the calling assembly.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddCQReetMediator(
        this IServiceCollection services,
        params Type[] assemblyMarkers
    ) {
        var requestWrappers = new Dictionary<Type, object>();
        var asyncRequestWrappers = new Dictionary<Type, object>();
        var notificationWrappers = new Dictionary<Type, NotificationWrapperBase>();

        var assemblies = assemblyMarkers.Length > 0
            ? assemblyMarkers.Select(t => t.Assembly).Distinct()
            : [Assembly.GetCallingAssembly()];

        var allTypes = assemblies
            .SelectMany(a => a.GetTypes())
            .Where(t => !t.IsInterface && !t.IsAbstract);

        foreach (var type in allTypes) {
            if (type.IsGenericTypeDefinition) {
                var interfaces = type.GetInterfaces();
                foreach (var interfaceType in interfaces) {
                    if (!interfaceType.IsGenericType) continue;

                    var genericDef = interfaceType.GetGenericTypeDefinition();

                    if (genericDef == typeof(IPipelineBehavior<,>)) {
                        services.AddTransient(typeof(IPipelineBehavior<,>), type);
                    } else if (genericDef == typeof(IAsyncPipelineBehavior<,>)) {
                        services.AddTransient(typeof(IAsyncPipelineBehavior<,>), type);
                    }


                }

                continue;
            }

            foreach (var interfaceType in type.GetInterfaces()) {
                if (!interfaceType.IsGenericType) continue;

                var genericDef = interfaceType.GetGenericTypeDefinition();
                var args = interfaceType.GetGenericArguments();

                if (genericDef == typeof(IRequestHandler<,>)) {
                    services.TryAddTransient(interfaceType, type);

                    var requestType = args[0];
                    var responseType = args[1];

                    if (!requestWrappers.ContainsKey(requestType)) {
                        var wrapperType = typeof(RequestWrapper<,>).MakeGenericType(requestType, responseType);
                        var wrapper = Activator.CreateInstance(wrapperType)!;
                        requestWrappers.Add(requestType, wrapper);
                    }
                } else if (genericDef == typeof(IAsyncRequestHandler<,>)) {
                    services.TryAddTransient(interfaceType, type);

                    var requestType = args[0];
                    var responseType = args[1];

                    if (!asyncRequestWrappers.ContainsKey(requestType)) {
                        var wrapperType = typeof(AsyncRequestWrapper<,>).MakeGenericType(requestType, responseType);
                        var wrapper = Activator.CreateInstance(wrapperType)!;
                        asyncRequestWrappers.Add(requestType, wrapper);
                    }
                } else if (genericDef == typeof(INotificationHandler<>)) {
                    services.AddTransient(interfaceType, type);

                    var notifType = args[0];
                    if (!notificationWrappers.ContainsKey(notifType)) {
                        var wrapperType = typeof(NotificationWrapper<>).MakeGenericType(notifType);
                        var wrapper = (NotificationWrapperBase)Activator.CreateInstance(wrapperType)!;
                        notificationWrappers.Add(notifType, wrapper);
                    }
                } else if (genericDef == typeof(IPipelineBehavior<,>) || genericDef == typeof(IAsyncPipelineBehavior<,>)) {
                    services.AddTransient(interfaceType, type);
                }
            }
        }

        var registry = new MediatorRegistry(requestWrappers, asyncRequestWrappers, notificationWrappers);
        services.AddSingleton(registry);
        services.AddScoped<IMediator, Mediator>();

        return services;
    }
}