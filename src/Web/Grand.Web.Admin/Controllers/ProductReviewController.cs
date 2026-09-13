using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Domain.Catalog;
using Grand.Web.Admin.Extensions;
using Grand.Web.AdminShared.Controllers;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.Common.Filters;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.Admin.Controllers;

// Reduced to a thin subclass of BaseProductReviewController (ARCH-001 ProductReview
// consolidation). All regions of behavior live in the shared base; this class only supplies
// Admin's DI wiring plus the attributes that used to arrive transitively via
// BaseAdminController - BaseProductReviewController can't inherit any single host's base
// controller. Same pattern as GiftVoucherController (see that file).
[AuthorizeAdmin]
[AutoValidateAntiforgeryToken]
[Area(Constants.AreaAdmin)]
[AuthorizeMenu]
public class ProductReviewController(
    IProductReviewViewModelService productReviewViewModelService,
    IProductReviewService productReviewService,
    ITranslationService translationService,
    IAdminDataScope<ProductReview> scope)
    : BaseProductReviewController(productReviewViewModelService, productReviewService, translationService, scope);
