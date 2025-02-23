using Grand.Business.Core.Interfaces.Catalog.Prices;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Business.Core.Interfaces.System.Reports;
using Grand.Domain.Permissions;
using Grand.Domain.Payments;
using Grand.Infrastructure;
using Grand.Web.Common.DataSource;
using Grand.Web.Common.Extensions;
using Grand.Web.Common.Localization;
using Grand.Web.Common.Security.Authorization;
using Grand.Web.Vendor.Models.Report;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Grand.Web.Vendor.Controllers;

[PermissionAuthorize(PermissionSystemName.Reports)]
public class ReportsController : BaseVendorController
{
    private readonly ICountryService _countryService;
    private readonly ICurrencyService _currencyService;
    private readonly ICustomerReportService _customerReportService;
    private readonly ICustomerService _customerService;
    private readonly IDateTimeService _dateTimeService;
    private readonly IOrderReportService _orderReportService;
    private readonly IOrderStatusService _orderStatusService;
    private readonly IPriceFormatter _priceFormatter;
    private readonly IProductAttributeFormatter _productAttributeFormatter;
    private readonly IProductService _productService;
    private readonly IProductsReportService _productsReportService;
    private readonly IStockQuantityService _stockQuantityService;
    private readonly ITranslationService _translationService;
    private readonly IContextAccessor _contextAccessor;
    private readonly IEnumTranslationService _enumTranslationService;
    public ReportsController(IOrderReportService orderReportService,
        IProductsReportService productsReportService,
        ICustomerReportService customerReportService,
        IContextAccessor contextAccessor,
        IPriceFormatter priceFormatter,
        IProductService productService,
        IProductAttributeFormatter productAttributeFormatter,
        IStockQuantityService stockQuantityService,
        ITranslationService translationService,
        ICountryService countryService,
        IDateTimeService dateTimeService,
        IOrderStatusService orderStatusService,
        ICurrencyService currencyService,
        ICustomerService customerService, 
        IEnumTranslationService enumTranslationService)
    {
        _orderReportService = orderReportService;
        _productsReportService = productsReportService;
        _customerReportService = customerReportService;
        _contextAccessor = contextAccessor;
        _priceFormatter = priceFormatter;
        _productService = productService;
        _productAttributeFormatter = productAttributeFormatter;
        _stockQuantityService = stockQuantityService;
        _translationService = translationService;
        _countryService = countryService;
        _dateTimeService = dateTimeService;
        _orderStatusService = orderStatusService;
        _currencyService = currencyService;
        _customerService = customerService;
        _enumTranslationService = enumTranslationService;
    }

    [NonAction]
    private async Task<DataSourceResult> GetBestsellersBriefReportModel(int pageIndex,
        int pageSize, int orderBy)
    {
        var items = await _orderReportService.BestSellersReport(
            vendorId: _contextAccessor.WorkContext.CurrentVendor.Id,
            orderBy: orderBy,
            pageIndex: pageIndex,
            pageSize: pageSize,
            showHidden: true);
        var result = new List<BestsellersReportLineModel>();
        foreach (var x in items)
        {
            var m = new BestsellersReportLineModel {
                ProductId = x.ProductId,
                TotalAmount =
                    _priceFormatter.FormatPrice(x.TotalAmount, await _currencyService.GetPrimaryStoreCurrency()),
                TotalQuantity = x.TotalQuantity
            };
            var product = await _productService.GetProductById(x.ProductId);
            if (product != null)
                m.ProductName = product.Name;
            result.Add(m);
        }

        var gridModel = new DataSourceResult {
            Data = result,
            Total = items.TotalCount
        };
        return gridModel;
    }

    [HttpPost]
    public async Task<IActionResult> BestsellersBriefReportByQuantityList(DataSourceRequest command)
    {
        var gridModel = await GetBestsellersBriefReportModel(command.Page - 1,
            command.PageSize, 1);

        return Json(gridModel);
    }

    [HttpPost]
    public async Task<IActionResult> BestsellersBriefReportByAmountList(DataSourceRequest command)
    {
        var gridModel = await GetBestsellersBriefReportModel(command.Page - 1,
            command.PageSize, 2);

        return Json(gridModel);
    }

    public async Task<IActionResult> BestsellersReport()
    {
        var model = new BestsellersReportModel {
            //payment statuses
            AvailablePaymentStatuses = _enumTranslationService.ToSelectList(PaymentStatus.Pending, false).ToList()
        };
        model.AvailablePaymentStatuses.Insert(0,
            new SelectListItem { Text = _translationService.GetResource("Vendor.Common.All"), Value = "" });

        //billing countries
        foreach (var c in await _countryService.GetAllCountriesForBilling(showHidden: true))
            model.AvailableCountries.Add(new SelectListItem { Text = c.Name, Value = c.Id });
        model.AvailableCountries.Insert(0,
            new SelectListItem { Text = _translationService.GetResource("Vendor.Common.All"), Value = "" });

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> BestsellersReportList(DataSourceRequest command, BestsellersReportModel model)
    {
        DateTime? startDateValue = model.StartDate == null
            ? null
            : _dateTimeService.ConvertToUtcTime(model.StartDate.Value, _dateTimeService.CurrentTimeZone);

        DateTime? endDateValue = model.EndDate == null
            ? null
            : _dateTimeService.ConvertToUtcTime(model.EndDate.Value, _dateTimeService.CurrentTimeZone).AddDays(1);

        var paymentStatus = model.PaymentStatusId > 0 ? (PaymentStatus?)model.PaymentStatusId : null;

        var items = await _orderReportService.BestSellersReport(
            createdFromUtc: startDateValue,
            createdToUtc: endDateValue,
            ps: paymentStatus,
            billingCountryId: model.BillingCountryId,
            orderBy: 2,
            vendorId: _contextAccessor.WorkContext.CurrentVendor.Id,
            pageIndex: command.Page - 1,
            pageSize: command.PageSize,
            showHidden: true,
            storeId: model.StoreId);

        var result = new List<BestsellersReportLineModel>();
        foreach (var x in items)
        {
            var m = new BestsellersReportLineModel {
                ProductId = x.ProductId,
                TotalAmount =
                    _priceFormatter.FormatPrice(x.TotalAmount, await _currencyService.GetPrimaryStoreCurrency()),
                TotalQuantity = x.TotalQuantity
            };
            var product = await _productService.GetProductById(x.ProductId);
            if (product != null)
                m.ProductName = product.Name;
            if (_contextAccessor.WorkContext.CurrentVendor != null)
            {
                if (product?.VendorId == _contextAccessor.WorkContext.CurrentVendor.Id)
                    result.Add(m);
            }
            else
            {
                result.Add(m);
            }
        }

        var gridModel = new DataSourceResult {
            Data = result,
            Total = items.TotalCount
        };

        return Json(gridModel);
    }

    public IActionResult NeverSoldReport()
    {
        var model = new NeverSoldReportModel();
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> NeverSoldReportList(DataSourceRequest command, NeverSoldReportModel model)
    {
        DateTime? startDateValue = model.StartDate == null
            ? null
            : _dateTimeService.ConvertToUtcTime(model.StartDate.Value, _dateTimeService.CurrentTimeZone);

        DateTime? endDateValue = model.EndDate == null
            ? null
            : _dateTimeService.ConvertToUtcTime(model.EndDate.Value, _dateTimeService.CurrentTimeZone).AddDays(1);


        var items = await _orderReportService.ProductsNeverSold("", _contextAccessor.WorkContext.CurrentVendor.Id,
            startDateValue, endDateValue,
            command.Page - 1, command.PageSize, true);
        var gridModel = new DataSourceResult {
            Data = items.Select(x =>
                new NeverSoldReportLineModel {
                    ProductId = x.Id,
                    ProductName = x.Name
                }),
            Total = items.TotalCount
        };

        return Json(gridModel);
    }

    public IActionResult CountryReport()
    {
        var model = new CountryReportModel {
            //payment statuses
            AvailablePaymentStatuses = _enumTranslationService.ToSelectList(PaymentStatus.Pending, false).ToList()
        };

        model.AvailablePaymentStatuses.Insert(0,
            new SelectListItem { Text = _translationService.GetResource("Vendor.Common.All"), Value = "" });

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> CountryReportList(CountryReportModel model)
    {
        DateTime? startDateValue = model.StartDate == null
            ? null
            : _dateTimeService.ConvertToUtcTime(model.StartDate.Value, _dateTimeService.CurrentTimeZone);

        DateTime? endDateValue = model.EndDate == null
            ? null
            : _dateTimeService.ConvertToUtcTime(model.EndDate.Value, _dateTimeService.CurrentTimeZone).AddDays(1);

        var paymentStatus = model.PaymentStatusId > 0 ? (PaymentStatus?)model.PaymentStatusId : null;

        var items = await _orderReportService.GetCountryReport(
            vendorId: _contextAccessor.WorkContext.CurrentVendor.Id,
            ps: paymentStatus,
            startTimeUtc: startDateValue,
            endTimeUtc: endDateValue);
        var result = new List<CountryReportLineModel>();
        foreach (var x in items)
        {
            var country = await _countryService.GetCountryById(!string.IsNullOrEmpty(x.CountryId) ? x.CountryId : "");
            var m = new CountryReportLineModel {
                CountryName = country != null ? country.Name : "Unknown",
                SumOrders = _priceFormatter.FormatPrice(x.SumOrders, await _currencyService.GetPrimaryStoreCurrency()),
                TotalOrders = x.TotalOrders
            };
            result.Add(m);
        }

        var gridModel = new DataSourceResult {
            Data = result,
            Total = items.Count
        };

        return Json(gridModel);
    }

    #region Low stock reports

    public IActionResult LowStockReport()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> LowStockReportList(DataSourceRequest command)
    {
        var lowStockProducts = await _productsReportService.LowStockProducts(_contextAccessor.WorkContext.CurrentVendor.Id);

        var models = new List<LowStockProductModel>();
        //products
        foreach (var product in lowStockProducts.products)
        {
            var lowStockModel = new LowStockProductModel {
                Id = product.Id,
                Name = product.Name,
                ManageInventoryMethod = _enumTranslationService.GetTranslationEnum(product.ManageInventoryMethodId),
                StockQuantity = _stockQuantityService.GetTotalStockQuantity(product, total: true),
                Published = product.Published
            };
            models.Add(lowStockModel);
        }

        //combinations
        foreach (var combination in lowStockProducts.combinations)
        {
            var product = await _productService.GetProductById(combination.ProductId);
            var lowStockModel = new LowStockProductModel {
                Id = product.Id,
                Name = product.Name,
                Attributes = await _productAttributeFormatter.FormatAttributes(product, combination.Attributes,
                    _contextAccessor.WorkContext.CurrentCustomer, "<br />", true, true, true, false),
                ManageInventoryMethod = _enumTranslationService.GetTranslationEnum(product.ManageInventoryMethodId),
                StockQuantity = combination.StockQuantity,
                Published = product.Published
            };
            models.Add(lowStockModel);
        }

        var gridModel = new DataSourceResult {
            Data = models.PagedForCommand(command),
            Total = models.Count
        };

        return Json(gridModel);
    }

    #endregion

    #region Customer Reports

    public async Task<IActionResult> Customer()
    {
        var model = new CustomerReportsModel();
        var status = await _orderStatusService.GetAll();

        model.AvailablePaymentStatuses = _enumTranslationService.ToSelectList(PaymentStatus.Pending, false).ToList();
        model.AvailablePaymentStatuses.Insert(0,
            new SelectListItem { Text = _translationService.GetResource("Vendor.Common.All"), Value = "" });

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> ReportBestCustomersByOrderTotalList(DataSourceRequest command,
        BestCustomersReportModel model)
    {
        DateTime? startDateValue = model.StartDate == null
            ? null
            : _dateTimeService.ConvertToUtcTime(model.StartDate.Value, _dateTimeService.CurrentTimeZone);

        DateTime? endDateValue = model.EndDate == null
            ? null
            : _dateTimeService.ConvertToUtcTime(model.EndDate.Value, _dateTimeService.CurrentTimeZone).AddDays(1);

        var paymentStatus = model.PaymentStatusId > 0 ? (PaymentStatus?)model.PaymentStatusId : null;

        var items = _customerReportService.GetBestCustomersReport("", _contextAccessor.WorkContext.CurrentVendor.Id, startDateValue,
            endDateValue,
            null, paymentStatus, null, 2, command.Page - 1, command.PageSize);

        var report = new List<BestCustomerReportLineModel>();
        foreach (var x in items)
        {
            var m = new BestCustomerReportLineModel {
                CustomerId = x.CustomerId,
                OrderTotal =
                    _priceFormatter.FormatPrice(x.OrderTotal, await _currencyService.GetPrimaryStoreCurrency()),
                OrderCount = x.OrderCount
            };
            var customer = await _customerService.GetCustomerById(x.CustomerId);
            if (customer != null)
                m.CustomerName = !string.IsNullOrEmpty(customer.Email)
                    ? customer.Email
                    : _translationService.GetResource("Vendor.Customers.Guest");
            report.Add(m);
        }

        var gridModel = new DataSourceResult {
            Data = report,
            Total = items.TotalCount
        };

        return Json(gridModel);
    }

    #endregion
}