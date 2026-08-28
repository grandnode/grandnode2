using Grand.Business.Core.Interfaces.Catalog.Directory;
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
using Grand.Web.Admin.Extensions;
using Grand.Web.AdminShared.Controllers;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.AdminShared.Models.Common;
using Grand.Web.Common.DataSource;
using Grand.Web.Common.Filters;
using Grand.Web.Common.Localization;
using Grand.Web.Common.Security.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.Admin.Controllers;

// Reduced to a thin subclass of BaseFullReportsController (ARCH-001 Reports consolidation). All 12
// shared + 8 Admin/Store-only actions live in the shared bases; this class supplies Admin's DI wiring,
// its own [Area]/[Authorize*]/[PermissionAuthorize] attributes (BaseFullReportsController can't
// inherit any single host's base controller - see this task's header note), the ManageCustomers
// overrides on CountryReport/Customer neither shared base carries (Tasks 5/6), and
// PopularSearchTermsReport, which stays declared here only (Task 10) - not on either shared base.
[AuthorizeAdmin]
[AutoValidateAntiforgeryToken]
[Area(Constants.AreaAdmin)]
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
    IPermissionService permissionService,
    ISearchTermService searchTermService)
    : BaseFullReportsController(orderReportService, productsReportService, customerReportViewModelService,
        priceFormatter, currencyService, productService, productAttributeFormatter, stockQuantityService,
        translationService, storeService, countryService, vendorService, dateTimeService, orderStatusService,
        enumTranslationService, contextAccessor, scope, orderService, customerReportService, permissionService)
{
    /// <summary>Admin-only ManageCustomers gate — absent on Store/Vendor. Global Constraint 5 names
    /// this check explicitly.</summary>
    public override async Task<IActionResult> CountryReport()
    {
        if (!await permissionService.Authorize(StandardPermission.ManageCustomers))
            return AccessDeniedView();
        return await base.CountryReport();
    }

    /// <summary>Admin-only ManageCustomers gate — absent on Store/Vendor. Confirmed present on Admin's
    /// original Customer() action (Task 5's header note flags that the spec's own §2.2 table omits
    /// this one) but not on Store's or Vendor's.</summary>
    public override async Task<IActionResult> Customer()
    {
        if (!await permissionService.Authorize(StandardPermission.ManageCustomers))
            return AccessDeniedView();
        return await base.Customer();
    }

    /// <summary>Admin-only, no Store/Vendor equivalent at all (Task 10) — not declared on either
    /// shared base.</summary>
    [HttpPost]
    public async Task<IActionResult> PopularSearchTermsReport(DataSourceRequest command)
    {
        if (!await permissionService.Authorize(StandardPermission.ManageProducts))
            return AccessDeniedView();

        var searchTermRecordLines = await searchTermService.GetStats(command.Page - 1, command.PageSize);
        var gridModel = new DataSourceResult {
            Data = searchTermRecordLines.Select(x => new SearchTermReportLineModel { Keyword = x.Keyword, Count = x.Count }),
            Total = searchTermRecordLines.TotalCount
        };
        return Json(gridModel);
    }
}
