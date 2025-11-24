using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using CQReetMediator.Abstractions;

namespace CQReetMediator.DependencyInjection;

public static class ServiceCollectionExtensions {
    public static IServiceCollection AddCQReetMediator(
        this IServiceCollection services,
        params Assembly[] assemblies) {
        RegisterHandlers(services, assemblies);
        RegisterPipelines(services, assemblies);

        services.AddSingleton<IMediator>(sp =>
            new Mediator(
                type => sp.GetService(type),
                type => sp.GetServices(type)
            )
        );

        return services;
    }

    private static void RegisterHandlers(IServiceCollection services, Assembly[] assemblies) {
        foreach (var type in assemblies.SelectMany(a => a.GetTypes())) {
            if (type.IsAbstract || type.IsInterface)
                continue;

            foreach (var i in type.GetInterfaces()) {
                if (!i.IsGenericType)
                    continue;

                var def = i.GetGenericTypeDefinition();

                if (def == typeof(IRequestHandler<,>) ||
                    def == typeof(IAsyncRequestHandler<,>) ||
                    def == typeof(INotificationHandler<>)) {
                    services.AddScoped(i, type);
                }
            }
        }
    }


    private static void RegisterPipelines(IServiceCollection services, Assembly[] assemblies) {
        foreach (var t in assemblies.SelectMany(a => a.GetTypes())) {
            if (t.IsAbstract || t.IsInterface)
                continue;

            foreach (var i in t.GetInterfaces()) {
                if (!i.IsGenericType)
                    continue;

                var def = i.GetGenericTypeDefinition();

                if (def == typeof(IPipelineBehavior<,>) ||
                    def == typeof(IAsyncPipelineBehavior<,>)) {
                    services.AddScoped(i, t);
                }
            }
        }
    }
}