using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Customers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Grand.Web.Common.RequestCustomerResolvers;

public static class GuestCustomerRegistration
{
    /// <summary>
    ///     Decorates the registered customer service so that the request's unsaved guest is saved before its first
    ///     write. Call after the business layer has registered ICustomerService
    /// </summary>
    public static IServiceCollection AddTransientGuestCustomers(this IServiceCollection services)
    {
        var registered = services.LastOrDefault(d => d.ServiceType == typeof(ICustomerService));
        if (registered?.ImplementationType == null)
            throw new InvalidOperationException(
                "The guest customer decorator needs a type-based ICustomerService registration made before it.");

        var innerType = registered.ImplementationType;
        services.Remove(registered);
        services.Add(new ServiceDescriptor(innerType, innerType, ServiceLifetime.Scoped));

        services.AddScoped<IGuestCustomerPersistence>(sp => new GuestCustomerPersistence(
            (ICustomerService)sp.GetRequiredService(innerType),
            sp.GetRequiredService<IGroupService>(),
            sp.GetRequiredService<ILogger<GuestCustomerPersistence>>()));
        services.Add(new ServiceDescriptor(typeof(ICustomerService), sp => new TransientGuestCustomerService(
            (ICustomerService)sp.GetRequiredService(innerType),
            sp.GetRequiredService<IGuestCustomerPersistence>()), ServiceLifetime.Scoped));
        return services;
    }
}
