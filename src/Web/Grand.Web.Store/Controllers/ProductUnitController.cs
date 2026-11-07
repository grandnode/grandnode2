using Grand.Business.Core.Interfaces.Catalog.Inventory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Checkout.Shipping;
using Grand.Business.Core.Interfaces.Common.Configuration;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Domain.Catalog;
using Grand.Domain.Shipping;
using Grand.Web.AdminShared.Controllers;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.AdminShared.Services;
using Grand.Web.Common.Filters;
using Grand.Web.Common.Localization;
using Microsoft.AspNetCore.Mvc;
using Grand.Web.Store.Extensions;

namespace Grand.Web.Store.Controllers;

[AuthorizeStore]
[AutoValidateAntiforgeryToken]
[Area(Constants.AreaStore)]
[AuthorizeMenu]
public class ProductUnitController(
    IProductUnitService productUnitService,
    IProductService productService,
    IWarehouseService warehouseService,
    IDateTimeService dateTimeService,
    IEnumTranslationService enumTranslationService,
    IRequestTranslationService translationService,
    IInventoryDocumentViewModelService inventoryDocumentViewModelService,
    IAdminDataScope<Product> productScope,
    ISettingService settingService)
    : BaseProductUnitController(productUnitService, productService, warehouseService, dateTimeService,
        enumTranslationService, translationService, inventoryDocumentViewModelService, productScope,
        settingService);
