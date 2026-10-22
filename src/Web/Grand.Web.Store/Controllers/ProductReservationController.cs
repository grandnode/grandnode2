using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Domain.Catalog;
using Grand.Web.AdminShared.Controllers;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.Common.Filters;
using Grand.Web.Common.Localization;
using Grand.Web.Store.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.Store.Controllers;

[AutoValidateAntiforgeryToken]
[Area(Constants.AreaStore)]
[AuthorizeStore]
[AuthorizeMenu]
public class ProductReservationController(
    IProductService productService,
    IRequestTranslationService translationService,
    IProductReservationService productReservationService,
    IEnumTranslationService enumTranslationService,
    IAdminDataScope<Product> scope)
    : BaseProductReservationController(productService, translationService, productReservationService, enumTranslationService, scope);
