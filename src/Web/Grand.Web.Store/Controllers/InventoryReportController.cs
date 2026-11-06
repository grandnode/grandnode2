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
public class InventoryReportController(
    IInventoryJournalService inventoryJournalService,
    IInventoryDocumentViewModelService inventoryDocumentViewModelService,
    IWarehouseService warehouseService,
    IProductService productService,
    IDateTimeService dateTimeService,
    IRequestTranslationService translationService,
    IAdminDataScope<Warehouse> warehouseScope,
    IAdminDataScope<Product> productScope)
    : BaseInventoryReportController(inventoryJournalService, inventoryDocumentViewModelService, warehouseService,
        productService, dateTimeService, translationService, warehouseScope, productScope);
