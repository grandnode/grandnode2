using Grand.Domain.Customers;

namespace Grand.Infrastructure.Configuration;

public static class AccessControlConfigExtensions
{
    /// <summary>
    ///     Get the customer group part of a cache key for data the ACL filters
    /// </summary>
    /// <param name="config">Access control config</param>
    /// <param name="customer">Customer</param>
    /// <returns>The sorted customer group identifiers; empty when the ACL is ignored, since the groups filter nothing then</returns>
    public static string GetCustomerGroupsCacheKey(this AccessControlConfig config, Customer customer)
    {
        ArgumentNullException.ThrowIfNull(config);

        return config.IgnoreAcl ? "" : customer.GetCustomerGroupsCacheKey();
    }
}
