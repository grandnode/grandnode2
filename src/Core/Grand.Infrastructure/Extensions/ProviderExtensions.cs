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
        if (method == null)
            throw new ArgumentNullException(nameof(method));

        return store == null || IsAuthenticateStore(method, store.Id);
    }

    public static bool IsAuthenticateStore(this IProvider method, string storeId)
    {
        if (method == null)
            throw new ArgumentNullException(nameof(method));

        if (string.IsNullOrEmpty(storeId))
            return true;

        if (method.LimitedToStores == null || !method.LimitedToStores.Any())
            return true;

        return method.LimitedToStores.Contains(storeId);
    }

    public static bool IsAuthenticateGroup(this IProvider method, Customer customer)
    {
        if (method == null)
            throw new ArgumentNullException(nameof(method));

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