using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using CQReetMediator.Abstractions;

namespace CQReetMediator.DependencyInjection;

public static class ServiceCollectionExtensions {
    public static IServiceCollection AddCQReetMediator(this IServiceCollection services, params Assembly[] assemblies) {
        RegisterHandlers(services, assemblies);
        RegisterPipelines(services, assemblies);

        services.AddSingleton<IServiceFactory, ServiceFactoryAdapter>();
        services.AddSingleton<IMediator, Mediator>();

        return services;
    }

    public static void RegisterHandlers(IServiceCollection services, params Assembly[] assemblies) {
        var handlerInterfaces = new[] {
            typeof(IRequestHandler<,>),
            typeof(IAsyncRequestHandler<,>),
            typeof(INotificationHandler<>)
        };

        foreach (var assembly in assemblies) {
            var types = assembly.GetTypes()
                .Where(t =>
                    t.IsClass &&
                    !t.IsAbstract &&
                    !t.IsGenericTypeDefinition
                );

            foreach (var type in types) {
                var implementedHandlerInterfaces = type
                    .GetInterfaces()
                    .Where(i =>
                        i.IsGenericType &&
                        handlerInterfaces.Contains(i.GetGenericTypeDefinition())
                    )
                    .ToList();

                foreach (var handlerInterface in implementedHandlerInterfaces) {
                    services.AddTransient(handlerInterface, type);
                }
            }
        }
    }


    public static void RegisterPipelines(IServiceCollection services, params Assembly[] assemblies) {
        foreach (var assembly in assemblies) {
            var pipelineTypes = assembly
                .GetTypes()
                .Where(t =>
                    t.IsClass &&
                    !t.IsAbstract &&
                    t.IsGenericTypeDefinition &&
                    t.GetInterfaces().Any(i =>
                        i.IsGenericType &&
                        (i.GetGenericTypeDefinition() == typeof(IPipelineBehavior<,>) ||
                         i.GetGenericTypeDefinition() == typeof(IAsyncPipelineBehavior<,>))
                    )
                );

            foreach (var pipeline in pipelineTypes) {
                if (pipeline.GetInterfaces()
                    .Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IPipelineBehavior<,>))) {
                    services.AddScoped(typeof(IPipelineBehavior<,>), pipeline);
                }

                if (pipeline.GetInterfaces()
                    .Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IAsyncPipelineBehavior<,>))) {
                    services.AddScoped(typeof(IAsyncPipelineBehavior<,>), pipeline);
                }
            }
        }
    }
}