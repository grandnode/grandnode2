using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Domain.Permissions;
using Grand.Web.AdminShared.Controllers;
using Grand.Web.AdminShared.Interfaces;
using Grand.Domain.Vendors;
using Grand.Web.Common.Security.Authorization;
using Grand.Web.Common.Filters;
using Grand.Web.Admin.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.Admin.Controllers;

[AuthorizeAdmin]
[Area(Constants.AreaAdmin)]
[AuthorizeMenu]
[PermissionAuthorize(PermissionSystemName.VendorReviews)]
[AutoValidateAntiforgeryToken]
public class VendorReviewController(
    IVendorViewModelService vendorViewModelService,
    IVendorService vendorService,
    ITranslationService translationService,
    IAdminDataScope<VendorReview> scope)
    : BaseVendorReviewController(vendorViewModelService, vendorService, translationService, scope)
{
    // Not duplicated by Vendor — exposing this to Vendor would leak other vendors' names/ids to a
    // vendor account (see ARCH-001 Phase 10 spec §2.2). Kept here rather than on the shared base.
    public async Task<IActionResult> VendorSearchAutoComplete(string term)
    {
        const int searchTermMinimumLength = 3;
        if (string.IsNullOrWhiteSpace(term) || term.Length < searchTermMinimumLength)
            return Content("");

        var vendors = await VendorService.SearchVendors(keywords: term);

        var result = (from p in vendors
                      select new
                      {
                          label = p.Name,
                          vendorid = p.Id
                      })
            .ToList();
        return Json(result);
    }
}
