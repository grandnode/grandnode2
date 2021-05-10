using Grand.Business.Catalog.Extensions;
using Grand.Business.Catalog.Interfaces.Prices;
using Grand.Business.Catalog.Interfaces.Products;
using Grand.Business.Checkout.Interfaces.Orders;
using Grand.Business.Common.Extensions;
using Grand.Business.Common.Interfaces.Directory;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Business.Common.Interfaces.Security;
using Grand.Business.Common.Interfaces.Stores;
using Grand.Business.Common.Services.Security;
using Grand.Business.Customers.Interfaces;
using Grand.Business.System.Interfaces.Reports;
using Grand.Business.System.Utilities;
using Grand.Domain.Orders;
using Grand.Domain.Payments;
using Grand.Domain.Shipping;
using Grand.Infrastructure;
using Grand.Web.Admin.Extensions;
using Grand.Web.Admin.Interfaces;
using Grand.Web.Admin.Models.Catalog;
using Grand.Web.Admin.Models.Common;
using Grand.Web.Admin.Models.Customers;
using Grand.Web.Admin.Models.Orders;
using Grand.Web.Common.DataSource;
using Grand.Web.Common.Extensions;
using Grand.Web.Common.Security.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Grand.Web.Admin.Controllers
{
    [PermissionAuthorize(PermissionSystemName.Reports)]
    public class ReportsController : BaseAdminController
    {
        private readonly IOrderService _orderService;
        private readonly IOrderReportService _orderReportService;
        private readonly IProductsReportService _productsReportService;
        private readonly ICustomerReportService _customerReportService;
        private readonly ICustomerReportViewModelService _customerReportViewModelService;
        private readonly IPermissionService _permissionService;
        private readonly IWorkContext _workContext;
        private readonly IPriceFormatter _priceFormatter;
        private readonly IProductService _productService;
        private readonly IProductAttributeFormatter _productAttributeFormatter;
        private readonly IStockQuantityService _stockQuantityService;
        private readonly ITranslationService _translationService;
        private readonly IStoreService _storeService;
        private readonly ICountryService _countryService;
        private readonly IVendorService _vendorService;
        private readonly IDateTimeService _dateTimeService;
        private readonly ISearchTermService _searchTermService;
        private readonly IGroupService _groupService;
        private readonly IOrderStatusService _orderStatusService;

        public ReportsController(IOrderService orderService,
        IOrderReportService orderReportService,
        IProductsReportService productsReportService,
        ICustomerReportService customerReportService,
        ICustomerReportViewModelService customerReportViewModelService,
        IPermissionService permissionService,
        IWorkContext workContext,
        IPriceFormatter priceFormatter,
        IProductService productService,
        IProductAttributeFormatter productAttributeFormatter,
        IStockQuantityService stockQuantityService,
        ITranslationService translationService,
        IStoreService storeService,
        ICountryService countryService,
        IVendorService vendorService,
        IDateTimeService dateTimeService,
        ISearchTermService searchTermService,
        IGroupService groupService,
        IOrderStatusService orderStatusService)
        {
            _orderService = orderService;
            _orderReportService = orderReportService;
            _productsReportService = productsReportService;
            _customerReportService = customerReportService;
            _customerReportViewModelService = customerReportViewModelService;
            _permissionService = permissionService;
            _workContext = workContext;
            _priceFormatter = priceFormatter;
            _productService = productService;
            _productAttributeFormatter = productAttributeFormatter;
            _stockQuantityService = stockQuantityService;
            _translationService = translationService;
            _storeService = storeService;
            _countryService = countryService;
            _vendorService = vendorService;
            _dateTimeService = dateTimeService;
            _searchTermService = searchTermService;
            _groupService = groupService;
            _orderStatusService = orderStatusService;
        }

        [NonAction]
        protected async Task<DataSourceResult> GetBestsellersBriefReportModel(int pageIndex,
            int pageSize, int orderBy)
        {
            //a vendor should have access only to his products
            string vendorId = "";
            if (_workContext.CurrentVendor != null && !await _groupService.IsStaff(_workContext.CurrentCustomer))
                vendorId = _workContext.CurrentVendor.Id;

            string storeId = "";
            if (await _groupService.IsStaff(_workContext.CurrentCustomer))
                storeId = _workContext.CurrentCustomer.StaffStoreId;

            var items = await _orderReportService.BestSellersReport(
                storeId: storeId,
                vendorId: vendorId,
                orderBy: orderBy,
                pageIndex: pageIndex,
                pageSize: pageSize,
                showHidden: true);
            var result = new List<BestsellersReportLineModel>();
            foreach (var x in items)
            {
                var m = new BestsellersReportLineModel
                {
                    ProductId = x.ProductId,
                    TotalAmount = _priceFormatter.FormatPrice(x.TotalAmount, false),
                    TotalQuantity = x.TotalQuantity,
                };
                var product = await _productService.GetProductById(x.ProductId);
                if (product != null)
                    m.ProductName = product.Name;
                result.Add(m);
            }

            var gridModel = new DataSourceResult
            {
                Data = result,
                Total = items.TotalCount
            };
            return gridModel;
        }

        [NonAction]
        protected virtual async Task<IList<OrderPeriodReportLineModel>> GetReportOrderPeriodModel()
        {
            var report = new List<OrderPeriodReportLineModel>();
            var reportperiod7days = await _orderReportService.GetOrderPeriodReport(7, _workContext.CurrentCustomer.StaffStoreId);
            report.Add(new OrderPeriodReportLineModel
            {
                Period = _translationService.GetResource("Admin.Reports.Period.7days"),
                Count = reportperiod7days.Count,
                Amount = reportperiod7days.Amount
            });

            var reportperiod14days = await _orderReportService.GetOrderPeriodReport(14, _workContext.CurrentCustomer.StaffStoreId);
            report.Add(new OrderPeriodReportLineModel
            {
                Period = _translationService.GetResource("Admin.Reports.Period.14days"),
                Count = reportperiod14days.Count,
                Amount = reportperiod14days.Amount
            });

            var reportperiodmonth = await _orderReportService.GetOrderPeriodReport(30, _workContext.CurrentCustomer.StaffStoreId);
            report.Add(new OrderPeriodReportLineModel
            {
                Period = _translationService.GetResource("Admin.Reports.Period.month"),
                Count = reportperiodmonth.Count,
                Amount = reportperiodmonth.Amount
            });

            var reportperiodyear = await _orderReportService.GetOrderPeriodReport(365, _workContext.CurrentCustomer.StaffStoreId);
            report.Add(new OrderPeriodReportLineModel
            {
                Period = _translationService.GetResource("Admin.Reports.Period.year"),
                Count = reportperiodyear.Count,
                Amount = reportperiodyear.Amount
            });

            return report;
        }


        [HttpPost]
        public async Task<IActionResult> BestsellersBriefReportByQuantityList(DataSourceRequest command)
        {
            if (!await _permissionService.Authorize(StandardPermission.ManageOrders))
                return Content("");

            var gridModel = await GetBestsellersBriefReportModel(command.Page - 1,
                command.PageSize, 1);

            return Json(gridModel);
        }

        [HttpPost]
        public async Task<IActionResult> BestsellersBriefReportByAmountList(DataSourceRequest command)
        {
            if (!await _permissionService.Authorize(StandardPermission.ManageOrders))
                return Content("");

            var gridModel = await GetBestsellersBriefReportModel(command.Page - 1,
                command.PageSize, 2);

            return Json(gridModel);
        }

        public async Task<IActionResult> BestsellersReport()
        {
            var model = new BestsellersReportModel
            {
                //vendor
                IsLoggedInAsVendor = _workContext.CurrentVendor != null && !await _groupService.IsStaff(_workContext.CurrentCustomer)
            };

            string storeId = "";
            if (await _groupService.IsStaff(_workContext.CurrentCustomer))
                storeId = _workContext.CurrentCustomer.StaffStoreId;

            //stores
            model.AvailableStores.Add(new SelectListItem { Text = _translationService.GetResource("Admin.Common.All"), Value = "" });
            foreach (var s in (await _storeService.GetAllStores()).Where(x => x.Id == storeId || string.IsNullOrWhiteSpace(storeId)))
                model.AvailableStores.Add(new SelectListItem { Text = s.Shortcut, Value = s.Id.ToString() });

            var status = await _orderStatusService.GetAll();
            //order statuses
            model.AvailableOrderStatuses = status.Select(x => new SelectListItem() { Value = x.StatusId.ToString(), Text = x.Name }).ToList();
            model.AvailableOrderStatuses.Insert(0, new SelectListItem { Text = _translationService.GetResource("Admin.Common.All"), Value = "" });

            //payment statuses
            model.AvailablePaymentStatuses = PaymentStatus.Pending.ToSelectList(HttpContext, false).ToList();
            model.AvailablePaymentStatuses.Insert(0, new SelectListItem { Text = _translationService.GetResource("Admin.Common.All"), Value = "" });

            //billing countries
            foreach (var c in await _countryService.GetAllCountriesForBilling(showHidden: true))
            {
                model.AvailableCountries.Add(new SelectListItem { Text = c.Name, Value = c.Id.ToString() });
            }
            model.AvailableCountries.Insert(0, new SelectListItem { Text = _translationService.GetResource("Admin.Common.All"), Value = "" });

            //vendors
            model.AvailableVendors.Add(new SelectListItem { Text = _translationService.GetResource("Admin.Common.All"), Value = "" });
            var vendors = await _vendorService.GetAllVendors(showHidden: true);
            foreach (var v in vendors)
                model.AvailableVendors.Add(new SelectListItem { Text = v.Name, Value = v.Id.ToString() });

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> BestsellersReportList(DataSourceRequest command, BestsellersReportModel model)
        {
            //a vendor should have access only to his products
            if (_workContext.CurrentVendor != null && !await _groupService.IsStaff(_workContext.CurrentCustomer))
            {
                model.VendorId = _workContext.CurrentVendor.Id;
            }

            if (await _groupService.IsStaff(_workContext.CurrentCustomer))
                model.StoreId = _workContext.CurrentCustomer.StaffStoreId;

            DateTime? startDateValue = (model.StartDate == null) ? null
                            : (DateTime?)_dateTimeService.ConvertToUtcTime(model.StartDate.Value, _dateTimeService.CurrentTimeZone);

            DateTime? endDateValue = (model.EndDate == null) ? null
                            : (DateTime?)_dateTimeService.ConvertToUtcTime(model.EndDate.Value, _dateTimeService.CurrentTimeZone).AddDays(1);

            int? orderStatus = model.OrderStatusId > 0 ? model.OrderStatusId : null;
            PaymentStatus? paymentStatus = model.PaymentStatusId > 0 ? (PaymentStatus?)(model.PaymentStatusId) : null;

            var items = await _orderReportService.BestSellersReport(
                createdFromUtc: startDateValue,
                createdToUtc: endDateValue,
                os: orderStatus,
                ps: paymentStatus,
                billingCountryId: model.BillingCountryId,
                orderBy: 2,
                vendorId: model.VendorId,
                pageIndex: command.Page - 1,
                pageSize: command.PageSize,
                showHidden: true,
                storeId: model.StoreId);

            var result = new List<BestsellersReportLineModel>();
            foreach (var x in items)
            {
                var m = new BestsellersReportLineModel
                {
                    ProductId = x.ProductId,
                    TotalAmount = _priceFormatter.FormatPrice(x.TotalAmount, false),
                    TotalQuantity = x.TotalQuantity,
                };
                var product = await _productService.GetProductById(x.ProductId);
                if (product != null)
                    m.ProductName = product.Name;
                if (_workContext.CurrentVendor != null)
                {
                    if (product.VendorId == _workContext.CurrentVendor.Id)
                        result.Add(m);
                }
                else
                    result.Add(m);
            }
            var gridModel = new DataSourceResult
            {
                Data = result,
                Total = items.TotalCount
            };

            return Json(gridModel);
        }

        [HttpPost]
        public async Task<IActionResult> ReportOrderPeriodList(DataSourceRequest command)
        {
            if (!await _permissionService.Authorize(StandardPermission.ManageOrders))
                return Content("");

            var model = await GetReportOrderPeriodModel();
            var gridModel = new DataSourceResult
            {
                Data = model,
                Total = model.Count
            };

            return Json(gridModel);
        }

        [HttpPost]
        public async Task<IActionResult> ReportOrderTimeChart(DataSourceRequest command, DateTime? startDate, DateTime? endDate)
        {
            if (!await _permissionService.Authorize(StandardPermission.ManageOrders))
                return Content("");

            string storeId = "";
            if (await _groupService.IsStaff(_workContext.CurrentCustomer))
                storeId = _workContext.CurrentCustomer.StaffStoreId;

            var model = await _orderReportService.GetOrderByTimeReport(storeId, startDate, endDate);
            var gridModel = new DataSourceResult
            {
                Data = model
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
            //a vendor should have access only to his products
            string vendorId = "";
            if (_workContext.CurrentVendor != null)
                vendorId = _workContext.CurrentVendor.Id;

            DateTime? startDateValue = (model.StartDate == null) ? null
                            : (DateTime?)_dateTimeService.ConvertToUtcTime(model.StartDate.Value, _dateTimeService.CurrentTimeZone);

            DateTime? endDateValue = (model.EndDate == null) ? null
                            : (DateTime?)_dateTimeService.ConvertToUtcTime(model.EndDate.Value, _dateTimeService.CurrentTimeZone).AddDays(1);

            string storeId = "";
            if (await _groupService.IsStaff(_workContext.CurrentCustomer))
                storeId = _workContext.CurrentCustomer.StaffStoreId;

            var items = await _orderReportService.ProductsNeverSold(storeId, vendorId,
                startDateValue, endDateValue,
                command.Page - 1, command.PageSize, true);
            var gridModel = new DataSourceResult
            {
                Data = items.Select(x =>
                    new NeverSoldReportLineModel
                    {
                        ProductId = x.Id,
                        ProductName = x.Name,
                    }),
                Total = items.TotalCount
            };

            return Json(gridModel);
        }

        [HttpPost]
        public async Task<IActionResult> OrderAverageReportList(DataSourceRequest command)
        {
            if (!await _permissionService.Authorize(StandardPermission.ManageOrders))
                return Content("");

            //a vendor does have access to this report
            if (_workContext.CurrentVendor != null && !await _groupService.IsStaff(_workContext.CurrentCustomer))
                return Content("");

            string storeId = "";
            if (await _groupService.IsStaff(_workContext.CurrentCustomer))
                storeId = _workContext.CurrentCustomer.StaffStoreId;

            var report = new List<OrderAverageReportLineSummary>
            {
                await _orderReportService.OrderAverageReport(storeId, (int)OrderStatusSystem.Pending),
                await _orderReportService.OrderAverageReport(storeId, (int)OrderStatusSystem.Processing),
                await _orderReportService.OrderAverageReport(storeId, (int)OrderStatusSystem.Complete),
                await _orderReportService.OrderAverageReport(storeId, (int)OrderStatusSystem.Cancelled)
            };

            var statuses = await _orderStatusService.GetAll();

            var model = report.Select(x => new OrderAverageReportLineSummaryModel
            {
                OrderStatus = statuses.FirstOrDefault(y => y.StatusId == x.OrderStatus)?.Name,
                SumTodayOrders = _priceFormatter.FormatPrice(x.SumTodayOrders, false),
                SumThisWeekOrders = _priceFormatter.FormatPrice(x.SumThisWeekOrders, false),
                SumThisMonthOrders = _priceFormatter.FormatPrice(x.SumThisMonthOrders, false),
                SumThisYearOrders = _priceFormatter.FormatPrice(x.SumThisYearOrders, false),
                SumAllTimeOrders = _priceFormatter.FormatPrice(x.SumAllTimeOrders, false),
            }).ToList();

            var gridModel = new DataSourceResult
            {
                Data = model,
                Total = model.Count
            };

            return Json(gridModel);
        }

        [HttpPost]
        public async Task<IActionResult> ReportLatestOrder(DataSourceRequest command, DateTime? startDate, DateTime? endDate)
        {
            if (!await _permissionService.Authorize(StandardPermission.ManageOrders))
                return Content("");

            //a vendor does have access to this report
            if (_workContext.CurrentVendor != null)
                return Content("");

            string storeId = "";
            if (await _groupService.IsStaff(_workContext.CurrentCustomer))
                storeId = _workContext.CurrentCustomer.StaffStoreId;

            //load orders
            var orders = await _orderService.SearchOrders(
                storeId: storeId,
                createdFromUtc: startDate,
                createdToUtc: endDate,
                pageIndex: command.Page - 1,
                pageSize: command.PageSize);

            var statuses = await _orderStatusService.GetAll();

            var items = new List<OrderModel>();
            foreach (var x in orders)
            {
                var store = await _storeService.GetStoreById(x.StoreId);
                items.Add(new OrderModel
                {
                    Id = x.Id,
                    OrderNumber = x.OrderNumber,
                    StoreName = store != null ? store.Shortcut : "Unknown",
                    OrderTotal = _priceFormatter.FormatPrice(x.OrderTotal, false),
                    OrderStatus = statuses.FirstOrDefault(y => y.StatusId == x.OrderStatusId)?.Name,
                    PaymentStatus = x.PaymentStatusId.GetTranslationEnum(_translationService, _workContext),
                    ShippingStatus = x.ShippingStatusId.GetTranslationEnum(_translationService, _workContext),
                    CustomerEmail = x.BillingAddress.Email,
                    CustomerFullName = string.Format("{0} {1}", x.BillingAddress.FirstName, x.BillingAddress.LastName),
                    CreatedOn = _dateTimeService.ConvertToUserTime(x.CreatedOnUtc, DateTimeKind.Utc)
                });
            }
            var gridModel = new DataSourceResult
            {
                Data = items,
                Total = orders.TotalCount
            };
            return Json(gridModel);
        }

        [HttpPost]
        public async Task<IActionResult> OrderIncompleteReportList(DataSourceRequest command)
        {
            if (!await _permissionService.Authorize(StandardPermission.ManageOrders))
                return Content("");

            //a vendor does have access to this report
            if (_workContext.CurrentVendor != null && !await _groupService.IsStaff(_workContext.CurrentCustomer))
                return Content("");

            string storeId = "";
            if (await _groupService.IsStaff(_workContext.CurrentCustomer))
                storeId = _workContext.CurrentCustomer.StaffStoreId;


            var model = new List<OrderIncompleteReportLineModel>();
            //not paid
            var psPending = await _orderReportService.GetOrderAverageReportLine(storeId: storeId, ps: PaymentStatus.Pending, ignoreCancelledOrders: true);
            model.Add(new OrderIncompleteReportLineModel
            {
                Item = _translationService.GetResource("Admin.Reports.Incomplete.TotalUnpaidOrders"),
                Count = psPending.CountOrders,
                Total = _priceFormatter.FormatPrice(psPending.SumOrders, false),
                ViewLink = Url.Action("List", "Order", new { paymentStatusId = ((int)PaymentStatus.Pending).ToString(), area = Constants.AreaAdmin })
            });
            //not shipped
            var ssPending = await _orderReportService.GetOrderAverageReportLine(storeId: storeId, ss: ShippingStatus.Pending, ignoreCancelledOrders: true);
            model.Add(new OrderIncompleteReportLineModel
            {
                Item = _translationService.GetResource("Admin.Reports.Incomplete.TotalNotShippedOrders"),
                Count = ssPending.CountOrders,
                Total = _priceFormatter.FormatPrice(ssPending.SumOrders, false),
                ViewLink = Url.Action("List", "Order", new { shippingStatusId = ((int)ShippingStatus.Pending).ToString(), area = Constants.AreaAdmin })
            });
            //pending
            var osPending = await _orderReportService.GetOrderAverageReportLine(storeId: storeId, os: (int)OrderStatusSystem.Pending, ignoreCancelledOrders: true);
            model.Add(new OrderIncompleteReportLineModel
            {
                Item = _translationService.GetResource("Admin.Reports.Incomplete.TotalIncompleteOrders"),
                Count = osPending.CountOrders,
                Total = _priceFormatter.FormatPrice(osPending.SumOrders, false),
                ViewLink = Url.Action("List", "Order", new { orderStatusId = ((int)OrderStatusSystem.Pending).ToString(), area = Constants.AreaAdmin })
            });

            var gridModel = new DataSourceResult
            {
                Data = model,
                Total = model.Count
            };

            return Json(gridModel);
        }

        public async Task<IActionResult> CountryReport()
        {
            if (!await _permissionService.Authorize(StandardPermission.ManageCustomers))
                return AccessDeniedView();

            var status = await _orderStatusService.GetAll();
            var model = new CountryReportModel
            {
                //order statuses
                AvailableOrderStatuses = status.Select(x => new SelectListItem() { Value = x.StatusId.ToString(), Text = x.Name }).ToList()
            };

            model.AvailableOrderStatuses.Insert(0, new SelectListItem { Text = _translationService.GetResource("Admin.Common.All"), Value = "" });

            //payment statuses
            model.AvailablePaymentStatuses = PaymentStatus.Pending.ToSelectList(HttpContext, false).ToList();
            model.AvailablePaymentStatuses.Insert(0, new SelectListItem { Text = _translationService.GetResource("Admin.Common.All"), Value = "" });

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> CountryReportList(DataSourceRequest command, CountryReportModel model)
        {
            DateTime? startDateValue = (model.StartDate == null) ? null
                            : (DateTime?)_dateTimeService.ConvertToUtcTime(model.StartDate.Value, _dateTimeService.CurrentTimeZone);

            DateTime? endDateValue = (model.EndDate == null) ? null
                            : (DateTime?)_dateTimeService.ConvertToUtcTime(model.EndDate.Value, _dateTimeService.CurrentTimeZone).AddDays(1);

            int? orderStatus = model.OrderStatusId > 0 ? model.OrderStatusId : null;
            PaymentStatus? paymentStatus = model.PaymentStatusId > 0 ? (PaymentStatus?)(model.PaymentStatusId) : null;

            string storeId = "";
            if (await _groupService.IsStaff(_workContext.CurrentCustomer))
                storeId = _workContext.CurrentCustomer.StaffStoreId;

            var items = await _orderReportService.GetCountryReport(
                storeId: storeId,
                os: orderStatus,
                ps: paymentStatus,
                startTimeUtc: startDateValue,
                endTimeUtc: endDateValue);
            var result = new List<CountryReportLineModel>();
            foreach (var x in items)
            {
                var country = await _countryService.GetCountryById(!String.IsNullOrEmpty(x.CountryId) ? x.CountryId : "");
                var m = new CountryReportLineModel
                {
                    CountryName = country != null ? country.Name : "Unknown",
                    SumOrders = _priceFormatter.FormatPrice(x.SumOrders, false),
                    TotalOrders = x.TotalOrders,
                };
                result.Add(m);
            }
            var gridModel = new DataSourceResult
            {
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
            string vendorId = "";
            //a vendor should have access only to his products
            if (_workContext.CurrentVendor != null && !await _groupService.IsStaff(_workContext.CurrentCustomer))
                vendorId = _workContext.CurrentVendor.Id;

            string storeId = "";
            if (await _groupService.IsStaff(_workContext.CurrentCustomer))
                storeId = _workContext.CurrentCustomer.StaffStoreId;

            var lowStockProducts = await _productsReportService.LowStockProducts(vendorId, storeId);

            var models = new List<LowStockProductModel>();
            //products
            foreach (var product in lowStockProducts.products)
            {
                var lowStockModel = new LowStockProductModel
                {
                    Id = product.Id,
                    Name = product.Name,
                    ManageInventoryMethod = product.ManageInventoryMethodId.GetTranslationEnum(_translationService, _workContext.WorkingLanguage.Id),
                    StockQuantity = _stockQuantityService.GetTotalStockQuantity(product, total: true),
                    Published = product.Published
                };
                models.Add(lowStockModel);
            }
            //combinations
            foreach (var combination in lowStockProducts.combinations)
            {
                var product = await _productService.GetProductById(combination.ProductId);
                var lowStockModel = new LowStockProductModel
                {
                    Id = product.Id,
                    Name = product.Name,
                    Attributes = await _productAttributeFormatter.FormatAttributes(product, combination.Attributes, _workContext.CurrentCustomer, "<br />", true, true, true, false),
                    ManageInventoryMethod = product.ManageInventoryMethodId.GetTranslationEnum(_translationService, _workContext.WorkingLanguage.Id),
                    StockQuantity = combination.StockQuantity,
                    Published = product.Published
                };
                models.Add(lowStockModel);
            }
            var gridModel = new DataSourceResult
            {
                Data = models.PagedForCommand(command),
                Total = models.Count
            };

            return Json(gridModel);
        }

        #endregion
        [HttpPost]
        public async Task<IActionResult> PopularSearchTermsReport(DataSourceRequest command)
        {
            if (!await _permissionService.Authorize(StandardPermission.ManageProducts))
                return AccessDeniedView();

            var searchTermRecordLines = await _searchTermService.GetStats(command.Page - 1, command.PageSize);
            var gridModel = new DataSourceResult
            {
                Data = searchTermRecordLines.Select(x => new SearchTermReportLineModel
                {
                    Keyword = x.Keyword,
                    Count = x.Count,
                }),
                Total = searchTermRecordLines.TotalCount
            };
            return Json(gridModel);
        }

        #region Customer Reports

        public async Task<IActionResult> Customer()
        {
            if (!await _permissionService.Authorize(StandardPermission.ManageCustomers))
                return AccessDeniedView();

            var model = await _customerReportViewModelService.PrepareCustomerReportsModel();
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> ReportBestCustomersByOrderTotalList(DataSourceRequest command, BestCustomersReportModel model)
        {
            if (await _groupService.IsStaff(_workContext.CurrentCustomer))
                model.StoreId = _workContext.CurrentCustomer.StaffStoreId;

            var (bestCustomerReportLineModels, totalCount) = await _customerReportViewModelService.PrepareBestCustomerReportLineModel(model, 1, command.Page, command.PageSize);
            var gridModel = new DataSourceResult
            {
                Data = bestCustomerReportLineModels.ToList(),
                Total = totalCount
            };
            return Json(gridModel);
        }

        [HttpPost]
        public async Task<IActionResult> ReportBestCustomersByNumberOfOrdersList(DataSourceRequest command, BestCustomersReportModel model)
        {
            if (await _groupService.IsStaff(_workContext.CurrentCustomer))
                model.StoreId = _workContext.CurrentCustomer.StaffStoreId;

            var (bestCustomerReportLineModels, totalCount) = await _customerReportViewModelService.PrepareBestCustomerReportLineModel(model, 2, command.Page, command.PageSize);
            var gridModel = new DataSourceResult
            {
                Data = bestCustomerReportLineModels.ToList(),
                Total = totalCount
            };
            return Json(gridModel);
        }

        [HttpPost]
        public async Task<IActionResult> ReportRegisteredCustomersList(DataSourceRequest command)
        {
            string storeId = "";
            if (await _groupService.IsStaff(_workContext.CurrentCustomer))
                storeId = _workContext.CurrentCustomer.StaffStoreId;

            var model = await _customerReportViewModelService.GetReportRegisteredCustomersModel(storeId);
            var gridModel = new DataSourceResult
            {
                Data = model,
                Total = model.Count
            };

            return Json(gridModel);
        }

        [HttpPost]
        public async Task<IActionResult> ReportCustomerTimeChart(DataSourceRequest command, DateTime? startDate, DateTime? endDate)
        {
            string storeId = "";
            if (await _groupService.IsStaff(_workContext.CurrentCustomer))
                storeId = _workContext.CurrentCustomer.StaffStoreId;

            var model = await _customerReportService.GetCustomerByTimeReport(storeId, startDate, endDate);
            var gridModel = new DataSourceResult
            {
                Data = model
            };
            return Json(gridModel);
        }
        #endregion
    }
}