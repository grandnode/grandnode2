using Grand.Domain.Vendors;
using Grand.Infrastructure;

namespace Grand.Web.AdminShared.Extensions;

public static class VendorPanelExtensions
{
    /// <summary>
    ///     The vendor the vendor panel works for: AuthorizeVendorAttribute lets in only a customer with a vendor
    /// </summary>
    public static Vendor PanelVendor(this IWorkContext workContext)
    {
        return workContext.CurrentVendor ??
               throw new InvalidOperationException("The vendor panel runs only for a customer with a vendor");
    }
}
