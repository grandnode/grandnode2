using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Domain.Catalog;
using Grand.Web.AdminShared.Controllers;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.Common.Filters;
using Grand.Web.Common.Localization;
using Grand.Web.Vendor.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.Vendor.Controllers;

[AutoValidateAntiforgeryToken]
[Area(Constants.AreaVendor)]
[AuthorizeVendor]
[AuthorizeMenu]
public class ProductPriceController(
    IProductPriceViewModelService productPriceViewModelService,
    IProductService productService,
    IRequestTranslationService translationService,
    IDateTimeService dateTimeService,
    IAdminDataScope<Product> scope)
    : BaseProductPriceController(productPriceViewModelService, productService, translationService, dateTimeService, scope);
