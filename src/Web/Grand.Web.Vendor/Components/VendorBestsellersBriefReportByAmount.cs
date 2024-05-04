using Grand.Web.Common.Components;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.Vendor.Components;

public class VendorBestsellersBriefReportByAmountViewComponent : BaseVendorViewComponent
{
    public IViewComponentResult Invoke()
    {
        return View();
    }
}