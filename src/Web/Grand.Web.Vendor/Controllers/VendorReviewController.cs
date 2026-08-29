using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Domain.Permissions;
using Grand.Domain.Vendors;
using Grand.Web.AdminShared.Controllers;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.Common.Security.Authorization;
using Grand.Web.Common.Filters;
using Grand.Web.Vendor.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.Vendor.Controllers;

[Area(Constants.AreaVendor)]
[AuthorizeVendor]
[AuthorizeMenu]
[PermissionAuthorize(PermissionSystemName.VendorReviews)]
[AutoValidateAntiforgeryToken]
public class VendorReviewController(
    IVendorViewModelService vendorViewModelService,
    IVendorService vendorService,
    ITranslationService translationService,
    IAdminDataScope<VendorReview> scope)
    : BaseVendorReviewController(vendorViewModelService, vendorService, translationService, scope);
