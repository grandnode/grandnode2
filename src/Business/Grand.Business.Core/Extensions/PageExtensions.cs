using Grand.Domain.Pages;

namespace Grand.Business.Core.Extensions;

public static class PageExtensions
{
    /// <summary>
    ///     Drops the page shared by every store wherever this store was given its own page under the same system name.
    ///     A store panel copies a shared page to edit it for one store, which leaves two pages carrying one system name
    ///     visible to that store; a list rendered for the storefront means the store's own one.
    /// </summary>
    /// <param name="pages">Pages already filtered to what the store may see</param>
    /// <param name="storeId">Store identifier; pass "" to keep every page</param>
    /// <returns>The pages to render for the store</returns>
    public static IList<Page> PreferStoreOverrides(this IEnumerable<Page> pages, string storeId)
    {
        ArgumentNullException.ThrowIfNull(pages);

        var all = pages as IList<Page> ?? pages.ToList();
        if (string.IsNullOrEmpty(storeId))
            return all;

        var overriddenSystemNames = all
            .Where(p => IsOwnedBy(p, storeId) && !string.IsNullOrEmpty(p.SystemName))
            .Select(p => p.SystemName)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (overriddenSystemNames.Count == 0)
            return all;

        return all
            .Where(p => IsOwnedBy(p, storeId) ||
                        string.IsNullOrEmpty(p.SystemName) ||
                        !overriddenSystemNames.Contains(p.SystemName))
            .ToList();
    }

    private static bool IsOwnedBy(Page page, string storeId)
    {
        return page.LimitedToStores && page.Stores.Contains(storeId);
    }
}
