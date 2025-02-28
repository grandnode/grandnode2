using Grand.Domain.Customers;
using Grand.Domain.Stores;
using Grand.Infrastructure.Plugins;
using Grand.SharedKernel.Extensions;
using System.Text.Json;

namespace Grand.Infrastructure.Extensions;

public static class ProviderExtensions
{
    public static bool IsAuthenticateStore(this IProvider method, Store store)
    {
        ArgumentNullException.ThrowIfNull(method);

        return store == null || IsAuthenticateStore(method, store.Id);
    }

    public static bool IsAuthenticateStore(this IProvider method, string storeId)
    {
        ArgumentNullException.ThrowIfNull(method);

        if (string.IsNullOrEmpty(storeId))
            return true;

        if (method.LimitedToStores == null || !method.LimitedToStores.Any())
            return true;

        return method.LimitedToStores.Contains(storeId);
    }

    public static bool IsAuthenticateGroup(this IProvider method, Customer customer)
    {
        ArgumentNullException.ThrowIfNull(method);

        if (customer == null)
            return true;

        if (method.LimitedToGroups == null || !method.LimitedToGroups.Any())
            return true;

        return method.LimitedToGroups.ContainsAny(customer.Groups.Select(x => x));
    }

    public static class JsonSerializerOptionsProvider
    {
        public static JsonSerializerOptions Options { get; } = new() {
            PropertyNameCaseInsensitive = true
        };
    }
}