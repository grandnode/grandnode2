using Grand.Infrastructure;

namespace Grand.Web.AdminShared.Extensions;

public static class StorePanelExtensions
{
    /// <summary>
    ///     The store the store panel works for: AuthorizeStoreAttribute lets in only a customer with a staff store
    /// </summary>
    public static string PanelStoreId(this IWorkContext workContext)
    {
        var storeId = workContext.CurrentCustomer.StaffStoreId;
        return !string.IsNullOrEmpty(storeId)
            ? storeId
            : throw new InvalidOperationException("The store panel runs only for a customer with a staff store");
    }
}
