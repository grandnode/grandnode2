using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Domain.Catalog;
using Grand.Web.Admin.Extensions;
using Grand.Web.AdminShared.Controllers;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.Common.Filters;
using Grand.Web.Common.Localization;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.Admin.Controllers;

[AuthorizeAdmin]
[AutoValidateAntiforgeryToken]
[Area(Constants.AreaAdmin)]
[AuthorizeMenu]
public class ProductPriceController(
    IProductViewModelService productViewModelService,
    IProductService productService,
    IRequestTranslationService translationService,
    IDateTimeService dateTimeService,
    IAdminDataScope<Product> scope)
    : BaseProductPriceController(productViewModelService, productService, translationService, dateTimeService, scope);
