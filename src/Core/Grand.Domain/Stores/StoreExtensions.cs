namespace Grand.Domain.Stores;

public static class StoreExtensions
{
    /// <summary>
    ///     Indicates whether a store contains a specified host
    /// </summary>
    /// <param name="store">Store</param>
    /// <param name="host">Host</param>
    /// <returns>true - contains, false - no</returns>
    public static bool ContainsHostValue(this Store store, string host)
    {
        ArgumentNullException.ThrowIfNull(store);

        if (string.IsNullOrEmpty(host))
            return false;

        var contains = store.Domains.FirstOrDefault(x => x.HostName.Equals(host, StringComparison.OrdinalIgnoreCase)) !=
                       null;
        return contains;
    }

    /// <summary>
    ///     Return domain host
    /// </summary>
    /// <param name="store">Store</param>
    /// <param name="host">Host</param>
    /// <returns>DomainHost</returns>
    public static DomainHost HostValue(this Store store, string host)
    {
        ArgumentNullException.ThrowIfNull(store);
        return string.IsNullOrEmpty(host)
            ? null
            : store.Domains.FirstOrDefault(x => x.HostName.Equals(host, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    ///     Return the host links to a store point to: its primary domain, else its first domain, else its url
    /// </summary>
    /// <param name="store">Store</param>
    /// <returns>DomainHost</returns>
    public static DomainHost PrimaryHost(this Store store)
    {
        ArgumentNullException.ThrowIfNull(store);
        return store.Domains?.FirstOrDefault(x => x.Primary) ?? store.Domains?.FirstOrDefault() ?? new DomainHost {
            Id = int.MinValue.ToString(),
            Url = store.Url,
            HostName = "temporary-store"
        };
    }

    /// <summary>
    ///     Return the host links to a store point to for a request to a given host: that host when it is one of the
    ///     store's domains, else the store's primary host
    /// </summary>
    /// <param name="store">Store</param>
    /// <param name="host">Host the request was made to; null when there is no request</param>
    /// <returns>DomainHost</returns>
    public static DomainHost LinkHost(this Store store, DomainHost host)
    {
        ArgumentNullException.ThrowIfNull(store);
        var domain = string.IsNullOrEmpty(host?.HostName)
            ? null
            : store.Domains?.FirstOrDefault(x => x.HostName.Equals(host.HostName, StringComparison.OrdinalIgnoreCase));
        return domain ?? store.PrimaryHost();
    }
}