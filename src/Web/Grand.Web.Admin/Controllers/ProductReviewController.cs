using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Domain.Catalog;
using Grand.Web.Admin.Extensions;
using Grand.Web.AdminShared.Controllers;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.Common.Filters;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.Admin.Controllers;

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
