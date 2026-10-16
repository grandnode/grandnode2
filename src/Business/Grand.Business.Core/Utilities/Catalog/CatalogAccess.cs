using Grand.Domain.Customers;

namespace Grand.Business.Core.Utilities.Catalog;

/// <summary>
///     Whose view of the catalog a read returns: the published records a customer's groups may see, or every
///     record, hidden and group-limited ones included, for the back office, import and the sitemap. A store id
///     keeps out the records limited to other stores in both cases. Two accesses are equal when they hold the same
///     customer and store.
/// </summary>
public sealed record CatalogAccess
{
    private CatalogAccess(Customer customer, string storeId)
    {
        Customer = customer;
        StoreId = storeId ?? "";
    }

    /// <summary>
    ///     Gets the customer whose groups limit the records; null when hidden records are included
    /// </summary>
    public Customer Customer { get; }

    /// <summary>
    ///     Gets the store whose limitations apply; empty when no store limits the records
    /// </summary>
    public string StoreId { get; }

    /// <summary>
    ///     Gets a value indicating whether unpublished and group-limited records are included
    /// </summary>
    public bool ShowHidden => Customer == null;

    /// <summary>
    ///     The published records the customer's groups may see in the store
    /// </summary>
    public static CatalogAccess For(Customer customer, string storeId)
    {
        ArgumentNullException.ThrowIfNull(customer);
        return new CatalogAccess(customer, storeId);
    }

    /// <summary>
    ///     Every record, hidden and group-limited ones included; a store id leaves out those limited to other stores
    /// </summary>
    public static CatalogAccess IncludingHidden(string storeId = "")
    {
        return new CatalogAccess(null, storeId);
    }
}
