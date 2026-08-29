using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Domain.Permissions;
using Grand.Domain.Vendors;
using Grand.Web.AdminShared.Controllers;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.Common.Security.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.Vendor.Controllers;

[PermissionAuthorize(PermissionSystemName.VendorReviews)]
[AutoValidateAntiforgeryToken]
public class VendorReviewController(
    IVendorViewModelService vendorViewModelService,
    IVendorService vendorService,
    ITranslationService translationService,
    IAdminDataScope<VendorReview> scope)
    : BaseVendorReviewController(vendorViewModelService, vendorService, translationService, scope);
