using Grand.Business.Core.Interfaces.Catalog.Inventory;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Checkout.Shipping;
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
public class ProductStockHistoryController(
    IProductService productService,
    IInventoryJournalService inventoryJournalService,
    IWarehouseService warehouseService,
    ICustomerService customerService,
    IEnumTranslationService enumTranslationService,
    IDateTimeService dateTimeService,
    IRequestTranslationService translationService,
    IAdminDataScope<Product> scope)
    : BaseProductStockHistoryController(productService, inventoryJournalService, warehouseService, customerService,
        enumTranslationService, dateTimeService, translationService, scope);
