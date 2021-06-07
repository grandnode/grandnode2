using Grand.Domain.Customers;
using Grand.Domain.Stores;
using Grand.Infrastructure.Plugins;
using Grand.SharedKernel.Extensions;
using System;
using System.Linq;

namespace Grand.Infrastructure.Extensions
{
    public static class ProviderExtensions
    {
        public static bool IsAuthenticateStore(this IProvider method, Store store)
        {
            if (method == null)
                throw new ArgumentNullException(nameof(method));

            if (store == null)
                return true;

            return IsAuthenticateStore(method, store.Id);
        }

        public static bool IsAuthenticateStore(this IProvider method, string storeId)
        {
            if (method == null)
                throw new ArgumentNullException(nameof(method));

            if (string.IsNullOrEmpty(storeId))
                return true;

            if (method.LimitedToStores == null || !method.LimitedToStores.Any())
                return true;

            if (!method.LimitedToStores.Contains(storeId))
                return false;

            return true;
        }

        public static bool IsAuthenticateGroup(this IProvider method, Customer customer)
        {
            if (method == null)
                throw new ArgumentNullException(nameof(method));

            if (customer == null)
                return true;

            if (method.LimitedToGroups == null || !method.LimitedToGroups.Any())
                return true;

            if (!method.LimitedToGroups.ContainsAny(customer.Groups.Select(x => x)))
                return false;

            return true;
        }
    }
}
