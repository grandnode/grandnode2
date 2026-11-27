using Microsoft.Extensions.DependencyInjection;

namespace Grand.Web.Common.Extensions;

public static class KeyedServiceHelper
{
    public static IList<string> GetKeyedServicesForInterface<TInterface>(IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        return services
            .Where(service => service.ServiceType == typeof(TInterface) && service.IsKeyedService)
            .Select(service => service.ServiceKey?.ToString())
            .OfType<string>()
            .ToList();
    }
}