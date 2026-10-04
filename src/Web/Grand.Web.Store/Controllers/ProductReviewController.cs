using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Domain.Catalog;
using Grand.Web.AdminShared.Controllers;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.Common.Filters;
using Grand.Web.Store.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.Store.Controllers;

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
