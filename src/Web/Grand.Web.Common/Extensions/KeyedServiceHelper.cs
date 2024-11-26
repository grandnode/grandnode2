using Microsoft.Extensions.DependencyInjection;

namespace Grand.Web.Common.Extensions;

public static class KeyedServiceHelper
{
    public static IList<string> GetKeyedServicesForInterface<TInterface>(IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        return services
            .Where(service =>
                service.ServiceType == typeof(TInterface) &&
                service.IsKeyedService &&
                service.ServiceKey != null)
            .Select(service => service.ServiceKey.ToString())
            .ToList();
    }
}