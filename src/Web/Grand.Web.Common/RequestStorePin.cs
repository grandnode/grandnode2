using Microsoft.AspNetCore.Http;

namespace Grand.Web.Common;

/// <summary>
///     Pins the store of a request. An authentication handler that validated a credential for one store sets it, so
///     <see cref="Middleware.ContextMiddleware" /> builds the store and work context in that store instead of the one
///     named by the store cookie or the host. Requests without a pin keep the usual order: cookie, host, first store.
/// </summary>
public static class RequestStorePin
{
    public const string ItemKey = "Grand.RequestStoreId";

    /// <summary>
    ///     Pins the request to the store; an empty id leaves the request unpinned
    /// </summary>
    public static void Set(HttpContext context, string storeId)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (!string.IsNullOrEmpty(storeId))
            context.Items[ItemKey] = storeId;
    }

    /// <summary>
    ///     The pinned store id, or null when the request is not pinned
    /// </summary>
    public static string Get(HttpContext context) =>
        context?.Items.TryGetValue(ItemKey, out var storeId) == true ? storeId as string : null;
}
