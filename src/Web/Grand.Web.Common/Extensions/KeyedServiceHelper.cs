using Microsoft.Extensions.DependencyInjection;

namespace Grand.Web.Common.Extensions;

public static class KeyedServiceHelper
{
    public static IList<string> GetKeyedServicesForInterface<TInterface>(IServiceCollection services)
    {
        var keyedServices = new List<string>();
        foreach (var service in services)
        {
            if (service.ServiceType == typeof(TInterface) && service.IsKeyedService)
            {
                keyedServices.Add(service.ServiceKey.ToString());
            }
        }
        return keyedServices;
    }
}