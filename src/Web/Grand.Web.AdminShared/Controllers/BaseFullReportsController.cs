#nullable enable

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
using Grand.Business.Core.Utilities.System;
using Grand.Domain.Orders;
using Grand.Domain.Payments;
using Grand.Domain.Permissions;
using Grand.Domain.Shipping;
using Grand.Infrastructure;
using Grand.Web.AdminShared.Interfaces;
using Grand.Web.AdminShared.Models.Orders;
using Grand.Web.Common.DataSource;
using Grand.Web.Common.Localization;
using Microsoft.AspNetCore.Mvc;

namespace Grand.Web.AdminShared.Controllers;

/// <summary>
///     The 8 report actions unique to Admin/Store (see ARCH-001 Reports consolidation spec §4).
///     Only Admin's and Store's concrete controllers inherit this class (Task 12) — Vendor's inherits
///     <see cref="BaseReportsController" /> directly, so none of these 8 actions, and neither of this
///     class's two <c>ManageOrders</c>-gated overrides of the inherited Bestsellers-brief actions
///     (Task 5's header note), become routable on the Vendor host.
/// </summary>
public abstract class BaseFullReportsController(
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
    : BaseReportsController(orderReportService, productsReportService, customerReportViewModelService,
        priceFormatter, currencyService, productService, productAttributeFormatter, stockQuantityService,
        translationService, storeService, countryService, vendorService, dateTimeService, orderStatusService,
        enumTranslationService, contextAccessor, scope)
{
    protected IOrderService OrderService => orderService;
    protected ICustomerReportService CustomerReportService => customerReportService;
    protected IPermissionService PermissionService => permissionService;

    #region Bestsellers-brief ManageOrders overrides

    /// <summary>Adds the ManageOrders check Admin/Store both have (identical between the two, so it
    /// belongs here once rather than duplicated per-host — see Task 5's header note) on top of
    /// BaseReportsController's check-free implementation. Never reached on the Vendor host — Vendor's
    /// thin subclass (Task 12) inherits BaseReportsController directly, not this class.</summary>
    public override async Task<IActionResult> BestsellersBriefReportByQuantityList(DataSourceRequest command)
    {
        if (!await permissionService.Authorize(StandardPermission.ManageOrders))
            return Content("");
        return await base.BestsellersBriefReportByQuantityList(command);
    }

    /// <summary>Same as above.</summary>
    public override async Task<IActionResult> BestsellersBriefReportByAmountList(DataSourceRequest command)
    {
        if (!await permissionService.Authorize(StandardPermission.ManageOrders))
            return Content("");
        return await base.BestsellersBriefReportByAmountList(command);
    }

    #endregion

    #region Order reports

    [NonAction]
    protected virtual async Task<IList<OrderPeriodReportLineModel>> GetReportOrderPeriodModel()
    {
        var report = new List<OrderPeriodReportLineModel>();
        foreach (var (days, resourceKey) in new (int, string)[] {
                     (7, "Admin.Reports.Period.7days"), (14, "Admin.Reports.Period.14days"),
                     (30, "Admin.Reports.Period.month"), (365, "Admin.Reports.Period.year")
                 })
        {
            var reportPeriod = await orderReportService.GetOrderPeriodReport(days, scope.StoreId);
            report.Add(new OrderPeriodReportLineModel {
                Period = translationService.GetResource(resourceKey),
                Count = reportPeriod.Count,
                Amount = reportPeriod.Amount
            });
        }

        return report;
    }

    [HttpPost]
    public virtual async Task<IActionResult> ReportOrderPeriodList(DataSourceRequest command)
    {
        if (!await permissionService.Authorize(StandardPermission.ManageOrders))
            return Content("");

        var model = await GetReportOrderPeriodModel();
        var gridModel = new DataSourceResult { Data = model, Total = model.Count };
        return Json(gridModel);
    }

    [HttpPost]
    public virtual async Task<IActionResult> ReportOrderTimeChart(DataSourceRequest command, DateTime? startDate,
        DateTime? endDate)
    {
        if (!await permissionService.Authorize(StandardPermission.ManageOrders))
            return Content("");

        var model = await orderReportService.GetOrderByTimeReport(scope.StoreId, startDate, endDate);
        return Json(new DataSourceResult { Data = model });
    }

    [HttpPost]
    public virtual async Task<IActionResult> OrderAverageReportList(DataSourceRequest command)
    {
        if (!await permissionService.Authorize(StandardPermission.ManageOrders))
            return Content("");

        var report = new List<OrderAverageReportLineSummary> {
            await orderReportService.OrderAverageReport(scope.StoreId, (int)OrderStatusSystem.Pending),
            await orderReportService.OrderAverageReport(scope.StoreId, (int)OrderStatusSystem.Processing),
            await orderReportService.OrderAverageReport(scope.StoreId, (int)OrderStatusSystem.Complete),
            await orderReportService.OrderAverageReport(scope.StoreId, (int)OrderStatusSystem.Cancelled)
        };

        var statuses = await orderStatusService.GetAll();
        var model = new List<OrderAverageReportLineSummaryModel>();
        foreach (var x in report)
            model.Add(new OrderAverageReportLineSummaryModel {
                OrderStatus = statuses.FirstOrDefault(y => y.StatusId == x.OrderStatus)?.Name,
                SumTodayOrders = priceFormatter.FormatPrice(x.SumTodayOrders, await currencyService.GetPrimaryStoreCurrency()),
                SumThisWeekOrders = priceFormatter.FormatPrice(x.SumThisWeekOrders, await currencyService.GetPrimaryStoreCurrency()),
                SumThisMonthOrders = priceFormatter.FormatPrice(x.SumThisMonthOrders, await currencyService.GetPrimaryStoreCurrency()),
                SumThisYearOrders = priceFormatter.FormatPrice(x.SumThisYearOrders, await currencyService.GetPrimaryStoreCurrency()),
                SumAllTimeOrders = priceFormatter.FormatPrice(x.SumAllTimeOrders, await currencyService.GetPrimaryStoreCurrency())
            });

        var gridModel = new DataSourceResult { Data = model, Total = model.Count };
        return Json(gridModel);
    }

    [HttpPost]
    public virtual async Task<IActionResult> ReportLatestOrder(DataSourceRequest command, DateTime? startDate,
        DateTime? endDate)
    {
        if (!await permissionService.Authorize(StandardPermission.ManageOrders))
            return Content("");

        var orders = await orderService.SearchOrders(
            storeId: scope.StoreId,
            createdFromUtc: startDate,
            createdToUtc: endDate,
            pageIndex: command.Page - 1,
            pageSize: command.PageSize);

        var statuses = await orderStatusService.GetAll();
        var items = new List<OrderModel>();
        foreach (var x in orders)
        {
            var store = await storeService.GetStoreById(x.StoreId);
            items.Add(new OrderModel {
                Id = x.Id,
                OrderNumber = x.OrderNumber,
                StoreName = store != null ? store.Shortcut : "Unknown",
                OrderTotal = priceFormatter.FormatPrice(x.OrderTotal, await currencyService.GetPrimaryStoreCurrency()),
                OrderStatus = statuses.FirstOrDefault(y => y.StatusId == x.OrderStatusId)?.Name,
                PaymentStatus = enumTranslationService.GetTranslationEnum(x.PaymentStatusId),
                ShippingStatus = enumTranslationService.GetTranslationEnum(x.ShippingStatusId),
                CustomerEmail = x.BillingAddress.Email,
                CustomerFullName = $"{x.BillingAddress.FirstName} {x.BillingAddress.LastName}",
                CreatedOn = dateTimeService.ConvertToUserTime(x.CreatedOnUtc, DateTimeKind.Utc)
            });
        }

        var gridModel = new DataSourceResult { Data = items, Total = orders.TotalCount };
        return Json(gridModel);
    }

    /// <summary>Area constant for the "View" link on each row: reproduces each host's original
    /// literal (Admin used Constants.AreaAdmin, Store used Constants.AreaStore) via
    /// scope.ResourceKeyPrefix, which happens to already equal "Admin" for both Admin and Store (Store
    /// reuses Admin's resource keys — Task 2's StoreReportDataScope) — but the *area* value must be
    /// the actual routing area, not the resource-key prefix, so this uses
    /// ViewContext.RouteData.Values["area"] directly instead, matching the same
    /// `ViewContext.RouteData.Values["area"]` pattern already used elsewhere in migrated AdminShared
    /// views/controllers (e.g. Grand.Web.AdminShared/Views/AdminShared/PaymentTransaction/Edit.cshtml)
    /// rather than introducing a third scope-object member just for this one Url.Action call.</summary>
    [HttpPost]
    public virtual async Task<IActionResult> OrderIncompleteReportList(DataSourceRequest command)
    {
        if (!await permissionService.Authorize(StandardPermission.ManageOrders))
            return Content("");

        var area = ControllerContext.RouteData.Values["area"]?.ToString();
        var model = new List<OrderIncompleteReportLineModel>();

        var psPending = await orderReportService.GetOrderAverageReportLine(scope.StoreId, ps: PaymentStatus.Pending,
            ignoreCancelledOrders: true);
        model.Add(new OrderIncompleteReportLineModel {
            Item = translationService.GetResource("Admin.Reports.Incomplete.TotalUnpaidOrders"),
            Count = psPending.CountOrders,
            Total = priceFormatter.FormatPrice(psPending.SumOrders, await currencyService.GetPrimaryStoreCurrency()),
            ViewLink = Url.Action("List", "Order", new { paymentStatusId = ((int)PaymentStatus.Pending).ToString(), area })
        });

        var ssPending = await orderReportService.GetOrderAverageReportLine(scope.StoreId, ss: ShippingStatus.Pending,
            ignoreCancelledOrders: true);
        model.Add(new OrderIncompleteReportLineModel {
            Item = translationService.GetResource("Admin.Reports.Incomplete.TotalNotShippedOrders"),
            Count = ssPending.CountOrders,
            Total = priceFormatter.FormatPrice(ssPending.SumOrders, await currencyService.GetPrimaryStoreCurrency()),
            ViewLink = Url.Action("List", "Order", new { shippingStatusId = ((int)ShippingStatus.Pending).ToString(), area })
        });

        var osPending = await orderReportService.GetOrderAverageReportLine(scope.StoreId, os: (int)OrderStatusSystem.Pending,
            ignoreCancelledOrders: true);
        model.Add(new OrderIncompleteReportLineModel {
            Item = translationService.GetResource("Admin.Reports.Incomplete.TotalIncompleteOrders"),
            Count = osPending.CountOrders,
            Total = priceFormatter.FormatPrice(osPending.SumOrders, await currencyService.GetPrimaryStoreCurrency()),
            ViewLink = Url.Action("List", "Order", new { orderStatusId = ((int)OrderStatusSystem.Pending).ToString(), area })
        });

        var gridModel = new DataSourceResult { Data = model, Total = model.Count };
        return Json(gridModel);
    }

    #endregion
}
