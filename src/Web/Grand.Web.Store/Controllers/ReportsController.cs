using Grand.Business.Core.Interfaces.Catalog.Prices;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Common.Security;
using Grand.Business.Core.Interfaces.Common.Stores;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Business.Core.Interfaces.System.Reports;
using Grand.Domain.Permissions;
using Grand.Infrastructure;
using Grand.Web.AdminShared.Controllers;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.Common.Filters;
using Grand.Web.Common.Localization;
using Grand.Web.Common.Security.Authorization;
using Grand.Web.Store.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.Store.Controllers;

// Reduced to a thin subclass of BaseFullReportsController (ARCH-001 Reports consolidation). No
// overrides needed - Store has neither the CountryReport/Customer ManageCustomers gate nor
// PopularSearchTermsReport; every remaining Admin/Store difference is already expressed inside the
// shared bases via IReportDataScope. Same pattern as PaymentTransactionController/OrderController.
[AutoValidateAntiforgeryToken]
[Area(Constants.AreaStore)]
[AuthorizeStore]
[AuthorizeMenu]
[PermissionAuthorize(PermissionSystemName.Reports)]
public class ReportsController(
    IOrderReportService orderReportService,
    IProductsReportService productsReportService,
    ICustomerReportViewModelService customerReportViewModelService,
    IPriceFormatter priceFormatter,
    ICurrencyService currencyService,
    IProductService productService,
    IProductAttributeFormatter productAttributeFormatter,
    IStockQuantityService stockQuantityService,
    ITranslationService translationService,
    IStoreService storeService,
    ICountryService countryService,
    IVendorService vendorService,
    IDateTimeService dateTimeService,
    IOrderStatusService orderStatusService,
    IEnumTranslationService enumTranslationService,
    IContextAccessor contextAccessor,
    IReportDataScope scope,
    IOrderService orderService,
    ICustomerReportService customerReportService,
    IPermissionService permissionService)
    : BaseFullReportsController(orderReportService, productsReportService, customerReportViewModelService,
        priceFormatter, currencyService, productService, productAttributeFormatter, stockQuantityService,
        translationService, storeService, countryService, vendorService, dateTimeService, orderStatusService,
        enumTranslationService, contextAccessor, scope, orderService, customerReportService, permissionService);
