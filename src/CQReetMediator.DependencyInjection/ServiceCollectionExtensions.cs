using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using CQReetMediator.Abstractions;

namespace CQReetMediator.DependencyInjection;

public static class ServiceCollectionExtensions {

    public static IServiceCollection AddCQReetMediator(
        this IServiceCollection services,
        params Type[] assemblyMarkers
    ) {
        var requestWrappers = new Dictionary<Type, object>();
        var voidRequestWrappers = new Dictionary<Type, object>();
        var notificationWrappers = new Dictionary<Type, NotificationWrapperBase>();

        var assemblies = assemblyMarkers.Length > 0
            ? assemblyMarkers.Select(t => t.Assembly).Distinct()
            : [Assembly.GetCallingAssembly()];

        var allTypes = assemblies
            .SelectMany(a => a.GetTypes())
            .Where(t => !t.IsInterface && !t.IsAbstract);

        foreach (var type in allTypes) {
            if (type.IsGenericTypeDefinition) {
                foreach (var interfaceType in type.GetInterfaces()) {
                    if (!interfaceType.IsGenericType) continue;
                    var genericDef = interfaceType.GetGenericTypeDefinition();

                    if (genericDef == typeof(IPipelineBehavior<,>))
                        services.AddTransient(typeof(IPipelineBehavior<,>), type);
                    else if (genericDef == typeof(IPipelineBehavior<>))
                        services.AddTransient(typeof(IPipelineBehavior<>), type);
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
                        requestWrappers.Add(requestType, Activator.CreateInstance(wrapperType)!);
                    }
                } else if (genericDef == typeof(IRequestHandler<>)) {
                    services.TryAddTransient(interfaceType, type);
                    var requestType = args[0];

                    if (!voidRequestWrappers.ContainsKey(requestType)) {
                        var wrapperType = typeof(VoidRequestWrapper<>).MakeGenericType(requestType);
                        voidRequestWrappers.Add(requestType, Activator.CreateInstance(wrapperType)!);
                    }
                } else if (genericDef == typeof(INotificationHandler<>)) {
                    services.AddTransient(interfaceType, type);
                    var notifType = args[0];

                    if (!notificationWrappers.ContainsKey(notifType)) {
                        var wrapperType = typeof(NotificationWrapper<>).MakeGenericType(notifType);
                        notificationWrappers.Add(notifType, (NotificationWrapperBase)Activator.CreateInstance(wrapperType)!);
                    }
                } else if (genericDef == typeof(IPipelineBehavior<,>) || genericDef == typeof(IPipelineBehavior<>)) {
                    services.AddTransient(interfaceType, type);
                }
            }
        }

        var registry = new MediatorRegistry(requestWrappers, voidRequestWrappers, notificationWrappers);
        services.AddSingleton(registry);
        services.AddSingleton<IMediator, Mediator>();

        return services;
    }
}
