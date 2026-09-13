using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Domain.Catalog;
using Grand.Web.AdminShared.Controllers;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.Common.Filters;
using Grand.Web.Store.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.Store.Controllers;

// Reduced to a thin subclass of BaseProductReviewController (ARCH-001 ProductReview
// consolidation). All regions of behavior live in the shared base; this class only supplies
// Store's DI wiring and the attributes that used to arrive transitively via
// BaseStoreController. No EditWarningCheck-style override needed: ProductReview has no
// "global" concept, so there's nothing for Store to warn about (see Task 1's scope doc
// comment).
[AutoValidateAntiforgeryToken]
[Area(Constants.AreaStore)]
[AuthorizeStore]
[AuthorizeMenu]
public class ProductReviewController(
    IProductReviewViewModelService productReviewViewModelService,
    IProductReviewService productReviewService,
    ITranslationService translationService,
    IAdminDataScope<ProductReview> scope)
    : BaseProductReviewController(productReviewViewModelService, productReviewService, translationService, scope);
