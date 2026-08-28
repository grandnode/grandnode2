using Grand.Business.Core.Interfaces.Catalog.Directory;
using Grand.Business.Core.Interfaces.Catalog.Prices;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
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
using Grand.Web.Vendor.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.Vendor.Controllers;

// Reduced to a thin subclass of BaseReportsController — deliberately NOT BaseFullReportsController
// (ARCH-001 Reports consolidation; Global Constraint 2/3). Vendor's 8-action surface is exactly the
// 12 shared actions minus the 4 it never had among the 12 (ReportOrderPeriodList/ReportOrderTimeChart/
// OrderAverageReportList/ReportLatestOrder/OrderIncompleteReportList/ReportBestCustomersByNumberOf-
// OrdersList/ReportRegisteredCustomersList/ReportCustomerTimeChart are all on BaseFullReportsController,
// never inherited here) - confirmed against the original controller's 8 actions read during plan
// research. No overrides needed: Vendor never had the ManageCustomers/ManageOrders inline checks on
// any of the 12 shared actions, which is exactly BaseReportsController's own check-free default.
[AutoValidateAntiforgeryToken]
[Area(Constants.AreaVendor)]
[AuthorizeVendor]
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
    IReportDataScope scope)
    : BaseReportsController(orderReportService, productsReportService, customerReportViewModelService,
        priceFormatter, currencyService, productService, productAttributeFormatter, stockQuantityService,
        translationService, storeService, countryService, vendorService, dateTimeService, orderStatusService,
        enumTranslationService, contextAccessor, scope);
