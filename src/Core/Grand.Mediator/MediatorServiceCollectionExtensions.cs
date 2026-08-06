using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Grand.Mediator;

public static class MediatorServiceCollectionExtensions
{
    private static readonly Type[] RequestHandlerTypes = [typeof(IRequestHandler<,>), typeof(IRequestHandler<>)];

    /// <summary>
    ///     Registers <see cref="IMediator" /> and every request/notification handler found in the assembly.
    /// </summary>
    /// <param name="services">Collection of service descriptors</param>
    /// <param name="assembly">Assembly to scan</param>
    /// <remarks>
    ///     Safe to call repeatedly, including for the same assembly - every registration goes through TryAdd.
    /// </remarks>
    public static IServiceCollection AddGrandMediator(this IServiceCollection services, Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(assembly);

        services.TryAddTransient<IMediator, Mediator>();

        foreach (var type in GetTypes(assembly))
        {
            if (!type.IsClass || type.IsAbstract) continue;

            //open generic handlers are skipped on purpose - the only one in the solution
            //(Grand.Module.Api GetGenericQueryHandler<T, C>) is registered by hand, closed over each entity pair
            if (type.ContainsGenericParameters) continue;

            foreach (var contract in type.GetInterfaces())
            {
                if (!contract.IsGenericType) continue;

                var definition = contract.GetGenericTypeDefinition();

                //a request has exactly one handler - the first registration wins
                if (RequestHandlerTypes.Contains(definition))
                    services.TryAddTransient(contract, type);

                //a notification has any number of handlers - TryAddEnumerable dedupes by (service, implementation)
                else if (definition == typeof(INotificationHandler<>))
                    services.TryAddEnumerable(ServiceDescriptor.Transient(contract, type));
            }
        }

        return services;
    }

    /// <summary>
    ///     Registers <see cref="IMediator" /> and every handler found in the assemblies.
    /// </summary>
    public static IServiceCollection AddGrandMediator(this IServiceCollection services,
        IEnumerable<Assembly> assemblies)
    {
        ArgumentNullException.ThrowIfNull(assemblies);

        foreach (var assembly in assemblies)
            services.AddGrandMediator(assembly);

        return services;
    }

    /// <summary>
    ///     A partially loadable assembly still yields the types that did load
    /// </summary>
    private static IEnumerable<Type> GetTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            return ex.Types.Where(type => type != null);
        }
    }
}
