#nullable enable

using Grand.Business.Core.Interfaces.Catalog.Directory;
using Grand.Business.Core.Interfaces.Catalog.Prices;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Common.Stores;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Business.Core.Interfaces.System.Reports;
using Grand.Business.Core.Utilities.System;
using Grand.Domain.Payments;
using Grand.Infrastructure;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.AdminShared.Models.Catalog;
using Grand.Web.AdminShared.Models.Customers;
using Grand.Web.AdminShared.Models.Orders;
using Grand.Web.Common.Controllers;
using Grand.Web.Common.DataSource;
using Grand.Web.Common.Extensions;
using Grand.Web.Common.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Grand.Web.AdminShared.Controllers;

/// <summary>
///     The 12 report actions all three hosts (Admin, Store, Vendor) share. See ARCH-001 Reports
///     consolidation spec §4. <see cref="Controllers.BaseFullReportsController" /> adds the 8 actions
///     unique to Admin/Store; Vendor's concrete controller inherits this class directly so those 8
///     never become routable on the Vendor host at all.
/// </summary>
public abstract class BaseReportsController(
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
    : BaseController
{
    // Exposed for BaseFullReportsController (Task 8/9) and for host-specific concrete-controller
    // overrides (Task 12) — same accessor pattern as BasePaymentTransactionController.
    protected IOrderReportService OrderReportService => orderReportService;
    protected IProductsReportService ProductsReportService => productsReportService;
    protected ICustomerReportViewModelService CustomerReportViewModelService => customerReportViewModelService;
    protected IPriceFormatter PriceFormatter => priceFormatter;
    protected ICurrencyService CurrencyService => currencyService;
    protected IProductService ProductService => productService;
    protected IProductAttributeFormatter ProductAttributeFormatter => productAttributeFormatter;
    protected IStockQuantityService StockQuantityService => stockQuantityService;
    protected ITranslationService TranslationService => translationService;
    protected IStoreService StoreService => storeService;
    protected ICountryService CountryService => countryService;
    protected IVendorService VendorService => vendorService;
    protected IDateTimeService DateTimeService => dateTimeService;
    protected IOrderStatusService OrderStatusService => orderStatusService;
    protected IEnumTranslationService EnumTranslationService => enumTranslationService;
    protected IContextAccessor ContextAccessor => contextAccessor;
    protected IReportDataScope Scope => scope;

    #region Bestsellers

    [NonAction]
    protected virtual async Task<DataSourceResult> GetBestsellersBriefReportModel(int pageIndex, int pageSize,
        int orderBy)
    {
        var items = await orderReportService.BestSellersReport(
            orderBy: orderBy,
            pageIndex: pageIndex,
            pageSize: pageSize,
            showHidden: true);
        var result = new List<BestsellersReportLineModel>();
        foreach (var x in items)
        {
            var m = new BestsellersReportLineModel {
                ProductId = x.ProductId,
                TotalAmount = priceFormatter.FormatPrice(x.TotalAmount, await currencyService.GetPrimaryStoreCurrency()),
                TotalQuantity = x.TotalQuantity
            };
            var product = await productService.GetProductById(x.ProductId);
            if (product != null)
                m.ProductName = product.Name;
            result.Add(m);
        }

        return new DataSourceResult { Data = result, Total = items.TotalCount };
    }

    /// <summary>No inline permission check here — matches Vendor's actual current behavior exactly
    /// (Vendor never had a ManageOrders gate on this action; Admin/Store do). `virtual` so
    /// BaseFullReportsController (Task 8) can override and add the check for exactly the two hosts
    /// that have it, without leaking it onto Vendor. See this task's header note.</summary>
    [HttpPost]
    public virtual async Task<IActionResult> BestsellersBriefReportByQuantityList(DataSourceRequest command)
    {
        var gridModel = await GetBestsellersBriefReportModel(command.Page - 1, command.PageSize, 1);
        return Json(gridModel);
    }

    /// <summary>Same as <see cref="BestsellersBriefReportByQuantityList" /> — no inline check here.</summary>
    [HttpPost]
    public virtual async Task<IActionResult> BestsellersBriefReportByAmountList(DataSourceRequest command)
    {
        var gridModel = await GetBestsellersBriefReportModel(command.Page - 1, command.PageSize, 2);
        return Json(gridModel);
    }

    /// <summary>Store/vendor picker population gated on scope.ShowStoreSelector/ShowVendorSelector —
    /// true for Admin only. AvailableOrderStatuses/AvailablePaymentStatuses/AvailableCountries are
    /// populated unconditionally for all three hosts: Vendor's pre-consolidation model never carried
    /// AvailableOrderStatuses at all, but populating it here is the same accepted "unused select list"
    /// tolerance already established for Store's AvailableVendors (spec §2.4/§5) — Vendor's view
    /// simply never renders the field. scope.ResourceKeyPrefix ("Admin" vs "Vendor") reproduces each
    /// host's original resource-key choice for the "(all)" placeholder text exactly (Admin/Store used
    /// "Admin.Common.All", Vendor used "Vendor.Common.All" — confirmed in the three original
    /// controllers).</summary>
    public virtual async Task<IActionResult> BestsellersReport()
    {
        var model = new BestsellersReportModel();

        if (scope.ShowStoreSelector)
        {
            model.AvailableStores.Add(new SelectListItem { Text = translationService.GetResource("Admin.Common.All"), Value = "" });
            foreach (var s in await storeService.GetAllStores())
                model.AvailableStores.Add(new SelectListItem { Text = s.Shortcut, Value = s.Id });
        }

        var status = await orderStatusService.GetAll();
        model.AvailableOrderStatuses = status.Select(x => new SelectListItem { Value = x.StatusId.ToString(), Text = x.Name }).ToList();
        model.AvailableOrderStatuses.Insert(0,
            new SelectListItem { Text = translationService.GetResource($"{scope.ResourceKeyPrefix}.Common.All"), Value = "" });

        model.AvailablePaymentStatuses = enumTranslationService.ToSelectList(PaymentStatus.Pending, false).ToList();
        model.AvailablePaymentStatuses.Insert(0,
            new SelectListItem { Text = translationService.GetResource($"{scope.ResourceKeyPrefix}.Common.All"), Value = "" });

        foreach (var c in await countryService.GetAllCountriesForBilling(showHidden: true))
            model.AvailableCountries.Add(new SelectListItem { Text = c.Name, Value = c.Id });
        model.AvailableCountries.Insert(0,
            new SelectListItem { Text = translationService.GetResource($"{scope.ResourceKeyPrefix}.Common.All"), Value = "" });

        if (scope.ShowVendorSelector)
        {
            model.AvailableVendors.Add(new SelectListItem { Text = translationService.GetResource("Admin.Common.All"), Value = "" });
            foreach (var v in await vendorService.GetAllVendors(showHidden: true))
                model.AvailableVendors.Add(new SelectListItem { Text = v.Name, Value = v.Id });
        }

        return View(model);
    }

    /// <summary>storeId/vendorId: scope value wins when non-empty (Store/Vendor force it), otherwise
    /// the posted model value is used unmodified (Admin, whose scope.StoreId/.VendorId are always "").
    /// Mirrors Store's original unconditional `model.StoreId = StaffStoreId` assignment exactly for
    /// Store, and Vendor's original explicit `vendorId: CurrentVendor.Id` argument exactly for Vendor,
    /// while leaving Admin's posted values untouched. Row filtering via scope.CanIncludeProduct
    /// reproduces Vendor's original `HasAccessToProduct` post-filter exactly (default-true for
    /// Admin/Store, including when product is null — same as the original "if CurrentVendor == null,
    /// always add" branch; VendorReportDataScope.CanIncludeProduct(null) is false, matching Vendor's
    /// original `product != null && HasAccessToProduct(product)` guard). model.StoreId/model.VendorId
    /// are normalized to "" when unposted (`null`, the default for an unbound `string` model property)
    /// because `IOrderReportService.BestSellersReport`'s storeId/vendorId parameters are non-nullable
    /// and default to "" — a `null` model value must not reach them as `null`.</summary>
    [HttpPost]
    public virtual async Task<IActionResult> BestsellersReportList(DataSourceRequest command, BestsellersReportModel model)
    {
        if (!string.IsNullOrEmpty(scope.StoreId)) model.StoreId = scope.StoreId;
        if (!string.IsNullOrEmpty(scope.VendorId)) model.VendorId = scope.VendorId;

        DateTime? startDateValue = model.StartDate == null
            ? null
            : dateTimeService.ConvertToUtcTime(model.StartDate.Value, dateTimeService.CurrentTimeZone);

        DateTime? endDateValue = model.EndDate == null
            ? null
            : dateTimeService.ConvertToUtcTime(model.EndDate.Value, dateTimeService.CurrentTimeZone).AddDays(1);

        int? orderStatus = model.OrderStatusId > 0 ? model.OrderStatusId : null;
        var paymentStatus = model.PaymentStatusId > 0 ? (PaymentStatus?)model.PaymentStatusId : null;

        var items = await orderReportService.BestSellersReport(
            storeId: model.StoreId ?? "",
            vendorId: model.VendorId ?? "",
            createdFromUtc: startDateValue,
            createdToUtc: endDateValue,
            os: orderStatus,
            ps: paymentStatus,
            billingCountryId: model.BillingCountryId ?? "",
            orderBy: 2,
            pageIndex: command.Page - 1,
            pageSize: command.PageSize,
            showHidden: true);

        var result = new List<BestsellersReportLineModel>();
        foreach (var x in items)
        {
            var m = new BestsellersReportLineModel {
                ProductId = x.ProductId,
                TotalAmount = priceFormatter.FormatPrice(x.TotalAmount, await currencyService.GetPrimaryStoreCurrency()),
                TotalQuantity = x.TotalQuantity
            };
            var product = await productService.GetProductById(x.ProductId);
            if (product != null)
                m.ProductName = product.Name;
            if (scope.CanIncludeProduct(product))
                result.Add(m);
        }

        var gridModel = new DataSourceResult { Data = result, Total = items.TotalCount };
        return Json(gridModel);
    }

    #endregion

    #region Never sold

    public virtual IActionResult NeverSoldReport() => View(new NeverSoldReportModel());

    /// <summary>storeId/vendorId: same "scope wins when non-empty, else posted model value" rule as
    /// BestsellersReportList — NeverSoldReportModel has no StoreId/VendorId fields of its own (none of
    /// the three original controllers ever posted one for this action), so the "posted value" side is
    /// always "" here; this reduces to "scope value, or '' for Admin", which is exactly each of the
    /// three originals' literal argument (Admin: "", ""; Store: storeId, ""; Vendor: "", vendorId).</summary>
    [HttpPost]
    public virtual async Task<IActionResult> NeverSoldReportList(DataSourceRequest command, NeverSoldReportModel model)
    {
        DateTime? startDateValue = model.StartDate == null
            ? null
            : dateTimeService.ConvertToUtcTime(model.StartDate.Value, dateTimeService.CurrentTimeZone);

        DateTime? endDateValue = model.EndDate == null
            ? null
            : dateTimeService.ConvertToUtcTime(model.EndDate.Value, dateTimeService.CurrentTimeZone).AddDays(1);

        var items = await orderReportService.ProductsNeverSold(scope.StoreId, scope.VendorId,
            startDateValue, endDateValue, command.Page - 1, command.PageSize, true);

        var gridModel = new DataSourceResult {
            Data = items.Select(x => new NeverSoldReportLineModel { ProductId = x.Id, ProductName = x.Name }),
            Total = items.TotalCount
        };
        return Json(gridModel);
    }

    #endregion

    #region Country

    /// <summary>No inline permission check here — matches Store's/Vendor's actual current behavior
    /// (only Admin gates this GET on ManageCustomers; see this task's header note and Global
    /// Constraint 5). `virtual` so Admin's thin subclass (Task 12) can override and add the check.</summary>
    public virtual async Task<IActionResult> CountryReport()
    {
        var status = await orderStatusService.GetAll();
        var model = new CountryReportModel {
            AvailableOrderStatuses = status.Select(x => new SelectListItem { Value = x.StatusId.ToString(), Text = x.Name }).ToList()
        };
        model.AvailableOrderStatuses.Insert(0,
            new SelectListItem { Text = translationService.GetResource($"{scope.ResourceKeyPrefix}.Common.All"), Value = "" });

        model.AvailablePaymentStatuses = enumTranslationService.ToSelectList(PaymentStatus.Pending, false).ToList();
        model.AvailablePaymentStatuses.Insert(0,
            new SelectListItem { Text = translationService.GetResource($"{scope.ResourceKeyPrefix}.Common.All"), Value = "" });

        return View(model);
    }

    [HttpPost]
    public virtual async Task<IActionResult> CountryReportList(DataSourceRequest command, CountryReportModel model)
    {
        DateTime? startDateValue = model.StartDate == null
            ? null
            : dateTimeService.ConvertToUtcTime(model.StartDate.Value, dateTimeService.CurrentTimeZone);

        DateTime? endDateValue = model.EndDate == null
            ? null
            : dateTimeService.ConvertToUtcTime(model.EndDate.Value, dateTimeService.CurrentTimeZone).AddDays(1);

        int? orderStatus = model.OrderStatusId > 0 ? model.OrderStatusId : null;
        var paymentStatus = model.PaymentStatusId > 0 ? (PaymentStatus?)model.PaymentStatusId : null;

        var items = await orderReportService.GetCountryReport(
            storeId: scope.StoreId,
            vendorId: scope.VendorId,
            os: orderStatus,
            ps: paymentStatus,
            startTimeUtc: startDateValue,
            endTimeUtc: endDateValue);

        var result = new List<CountryReportLineModel>();
        foreach (var x in items)
        {
            var country = await countryService.GetCountryById(!string.IsNullOrEmpty(x.CountryId) ? x.CountryId : "");
            result.Add(new CountryReportLineModel {
                CountryName = country != null ? country.Name : "Unknown",
                SumOrders = priceFormatter.FormatPrice(x.SumOrders, await currencyService.GetPrimaryStoreCurrency()),
                TotalOrders = x.TotalOrders
            });
        }

        var gridModel = new DataSourceResult { Data = result, Total = items.Count };
        return Json(gridModel);
    }

    #endregion

    #region Low stock reports

    public virtual IActionResult LowStockReport() => View();

    [HttpPost]
    public virtual async Task<IActionResult> LowStockReportList(DataSourceRequest command)
    {
        var lowStockProducts = await productsReportService.LowStockProducts(scope.VendorId, scope.StoreId);

        var models = new List<LowStockProductModel>();
        foreach (var product in lowStockProducts.products)
            models.Add(new LowStockProductModel {
                Id = product.Id,
                Name = product.Name,
                ManageInventoryMethod = enumTranslationService.GetTranslationEnum(product.ManageInventoryMethodId),
                StockQuantity = stockQuantityService.GetTotalStockQuantity(product, total: true),
                Published = product.Published
            });

        foreach (var combination in lowStockProducts.combinations)
        {
            var product = await productService.GetProductById(combination.ProductId);
            models.Add(new LowStockProductModel {
                Id = product.Id,
                Name = product.Name,
                Attributes = await productAttributeFormatter.FormatAttributes(product, combination.Attributes,
                    contextAccessor.WorkContext.CurrentCustomer, "<br />", true, true, true, false),
                ManageInventoryMethod = enumTranslationService.GetTranslationEnum(product.ManageInventoryMethodId),
                StockQuantity = combination.StockQuantity,
                Published = product.Published
            });
        }

        var gridModel = new DataSourceResult { Data = models.PagedForCommand(command), Total = models.Count };
        return Json(gridModel);
    }

    #endregion

    #region Customer reports

    /// <summary>No inline permission check here — matches Store's/Vendor's actual current behavior
    /// (only Admin gates this GET on ManageCustomers; see Task 5's header note and Global Constraint
    /// 5, which names CountryReport's twin of this same check explicitly but not this one). `virtual`
    /// so Admin's thin subclass (Task 12) can override and add the check. Reuses
    /// PrepareCustomerReportsModel() as-is for all three hosts, including Vendor, accepting the
    /// unused-select-list over-population documented at spec §5/§2.4 as a disclosed, accepted
    /// tradeoff rather than adding a Vendor-specific thinner variant.</summary>
    public virtual async Task<IActionResult> Customer()
    {
        var model = await customerReportViewModelService.PrepareCustomerReportsModel();
        return View(model);
    }

    /// <summary>`scope.VendorId` is the new 5th argument added to
    /// `PrepareBestCustomerReportLineModel` by Task 11 (Global Constraint 8) — "" for Admin/Store
    /// (unchanged behavior), the current vendor's id for Vendor (replaces Vendor's original inline
    /// `GetBestCustomersReport("", vendorId, ...)` reimplementation entirely; see Task 11).
    /// model.StoreId is still threaded the same "scope wins when non-empty" way as every other action
    /// in this file, for the storeId half of the same call.</summary>
    [HttpPost]
    public virtual async Task<IActionResult> ReportBestCustomersByOrderTotalList(DataSourceRequest command,
        BestCustomersReportModel model)
    {
        if (!string.IsNullOrEmpty(scope.StoreId)) model.StoreId = scope.StoreId;

        var (bestCustomerReportLineModels, totalCount) = await customerReportViewModelService
            .PrepareBestCustomerReportLineModel(model, 1, command.Page, command.PageSize, scope.VendorId);

        var gridModel = new DataSourceResult { Data = bestCustomerReportLineModels.ToList(), Total = totalCount };
        return Json(gridModel);
    }

    #endregion
}
