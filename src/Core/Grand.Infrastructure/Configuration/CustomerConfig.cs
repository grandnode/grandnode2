namespace Grand.Infrastructure.Configuration;

/// <summary>
///     Represents a Customer Config (bound from the "Customer" section of appsettings.json)
/// </summary>
public class CustomerConfig
{
    /// <summary>
    ///     Gets or sets a value indicating whether customer accounts are scoped per store.
    ///     <para>
    ///         When <c>false</c> (default), the customer uniqueness key is the e-mail address (and username)
    ///         alone — an e-mail can only ever belong to a single customer across the whole installation.
    ///     </para>
    ///     <para>
    ///         When <c>true</c>, the uniqueness key becomes the pair (e-mail / username + <c>StoreId</c>).
    ///         The same e-mail address may therefore be registered independently in two different stores as
    ///         two separate, unrelated accounts. This affects: customer registration, storefront login,
    ///         the re-resolution of the authenticated customer from the auth cookie, password recovery and
    ///         the duplicate-checks performed by the Admin / Store-manager customer editors.
    ///     </para>
    ///     <para>
    ///         NOTE: customers are still stored in a single collection (they are discriminated by the
    ///         <c>StoreId</c> field — there is no separate collection per store). Uniqueness is enforced at
    ///         the application layer; the customer lookup index is the compound (Email + StoreId) index.
    ///         Changing this value on a live installation that already contains duplicate e-mails across
    ///         stores can make some accounts unreachable, so set it before going live.
    ///     </para>
    /// </summary>
    public bool RegisterCustomersPerStore { get; set; }
}
