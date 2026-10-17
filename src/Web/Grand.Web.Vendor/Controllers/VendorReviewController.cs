using Grand.Business.Core.Interfaces.Customers;
using Grand.Domain.Permissions;
using Grand.Domain.Vendors;
using Grand.Web.AdminShared.Controllers;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.Common.Security.Authorization;
using Grand.Web.Common.Filters;
using Grand.Web.Vendor.Extensions;
using Microsoft.AspNetCore.Mvc;
using Grand.Web.Common.Localization;

namespace Grand.Web.Vendor.Controllers;

[Area(Constants.AreaVendor)]
[AuthorizeVendor]
[AuthorizeMenu]
[PermissionAuthorize(PermissionSystemName.VendorReviews)]
[AutoValidateAntiforgeryToken]
public class VendorReviewController(
    IVendorViewModelService vendorViewModelService,
    IVendorService vendorService,
    IRequestTranslationService translationService,
    IAdminDataScope<VendorReview> scope)
    : BaseVendorReviewController(vendorViewModelService, vendorService, translationService, scope);
