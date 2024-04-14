using Grand.Web.Common.Components;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.Vendor.Components;

public class VendorBestsellersBriefReportByQuantityViewComponent : BaseVendorViewComponent
{
    public IViewComponentResult Invoke()
    {
        return View();
    }
}