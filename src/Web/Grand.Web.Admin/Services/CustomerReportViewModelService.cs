using Grand.Business.Core.Interfaces.Catalog.Prices;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Business.Core.Interfaces.System.Reports;
using Grand.Domain.Payments;
using Grand.Domain.Shipping;
using Grand.Infrastructure;
using Grand.Web.Admin.Interfaces;
using Grand.Web.Admin.Models.Customers;
using Grand.Web.Common.Extensions;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Grand.Web.Admin.Services;

public class CustomerReportViewModelService : ICustomerReportViewModelService
{
    private readonly ICurrencyService _currencyService;
    private readonly ICustomerReportService _customerReportService;
    private readonly ICustomerService _customerService;
    private readonly IDateTimeService _dateTimeService;
    private readonly IOrderStatusService _orderStatusService;
    private readonly IPriceFormatter _priceFormatter;
    private readonly ITranslationService _translationService;
    private readonly IWorkContext _workContext;

    public CustomerReportViewModelService(IWorkContext workContext,
        ICustomerService customerService,
        ITranslationService translationService, ICustomerReportService customerReportService,
        IDateTimeService dateTimeService, IPriceFormatter priceFormatter,
        IOrderStatusService orderStatusService, ICurrencyService currencyService)
    {
        _workContext = workContext;
        _customerService = customerService;
        _translationService = translationService;
        _customerReportService = customerReportService;
        _dateTimeService = dateTimeService;
        _priceFormatter = priceFormatter;
        _orderStatusService = orderStatusService;
        _currencyService = currencyService;
    }

    public virtual async Task<CustomerReportsModel> PrepareCustomerReportsModel()
    {
        var model = new CustomerReportsModel {
            //customers by number of orders
            BestCustomersByNumberOfOrders = new BestCustomersReportModel()
        };
        var status = await _orderStatusService.GetAll();

        model.BestCustomersByNumberOfOrders.AvailableOrderStatuses = status
            .Select(x => new SelectListItem { Value = x.StatusId.ToString(), Text = x.Name }).ToList();

        model.BestCustomersByNumberOfOrders.AvailableOrderStatuses.Insert(0,
            new SelectListItem { Text = _translationService.GetResource("Admin.Common.All"), Value = "" });
        model.BestCustomersByNumberOfOrders.AvailablePaymentStatuses =
            PaymentStatus.Pending.ToSelectList(_translationService, _workContext, false).ToList();
        model.BestCustomersByNumberOfOrders.AvailablePaymentStatuses.Insert(0,
            new SelectListItem { Text = _translationService.GetResource("Admin.Common.All"), Value = "" });
        model.BestCustomersByNumberOfOrders.AvailableShippingStatuses = ShippingStatus.Pending
            .ToSelectList(_translationService, _workContext, false).ToList();
        model.BestCustomersByNumberOfOrders.AvailableShippingStatuses.Insert(0,
            new SelectListItem { Text = _translationService.GetResource("Admin.Common.All"), Value = "" });

        //customers by order total
        model.BestCustomersByOrderTotal = new BestCustomersReportModel {
            AvailableOrderStatuses =
                status.Select(x => new SelectListItem { Value = x.StatusId.ToString(), Text = x.Name }).ToList()
        };
        model.BestCustomersByOrderTotal.AvailableOrderStatuses.Insert(0,
            new SelectListItem { Text = _translationService.GetResource("Admin.Common.All"), Value = "" });
        model.BestCustomersByOrderTotal.AvailablePaymentStatuses =
            PaymentStatus.Pending.ToSelectList(_translationService, _workContext, false).ToList();
        model.BestCustomersByOrderTotal.AvailablePaymentStatuses.Insert(0,
            new SelectListItem { Text = _translationService.GetResource("Admin.Common.All"), Value = "" });
        model.BestCustomersByOrderTotal.AvailableShippingStatuses = ShippingStatus.Pending
            .ToSelectList(_translationService, _workContext, false).ToList();
        model.BestCustomersByOrderTotal.AvailableShippingStatuses.Insert(0,
            new SelectListItem { Text = _translationService.GetResource("Admin.Common.All"), Value = "" });

        return model;
    }

    public virtual async Task<IList<RegisteredCustomerReportLineModel>> GetReportRegisteredCustomersModel(
        string storeId)
    {
        var report = new List<RegisteredCustomerReportLineModel> {
            new() {
                Period = _translationService.GetResource(
                    "Admin.Reports.Customers.RegisteredCustomers.Fields.Period.7days"),
                Customers = await _customerReportService.GetRegisteredCustomersReport(storeId, 7)
            },

            new() {
                Period = _translationService.GetResource(
                    "Admin.Reports.Customers.RegisteredCustomers.Fields.Period.14days"),
                Customers = await _customerReportService.GetRegisteredCustomersReport(storeId, 14)
            },
            new() {
                Period = _translationService.GetResource(
                    "Admin.Reports.Customers.RegisteredCustomers.Fields.Period.month"),
                Customers = await _customerReportService.GetRegisteredCustomersReport(storeId, 30)
            },
            new() {
                Period = _translationService.GetResource(
                    "Admin.Reports.Customers.RegisteredCustomers.Fields.Period.year"),
                Customers = await _customerReportService.GetRegisteredCustomersReport(storeId, 365)
            }
        };

        return report;
    }

    public virtual async Task<(IEnumerable<BestCustomerReportLineModel> bestCustomerReportLineModels, int totalCount)>
        PrepareBestCustomerReportLineModel(BestCustomersReportModel model, int orderBy, int pageIndex, int pageSize)
    {
        DateTime? startDateValue = model.StartDate == null
            ? null
            : _dateTimeService.ConvertToUtcTime(model.StartDate.Value, _dateTimeService.CurrentTimeZone);

        DateTime? endDateValue = model.EndDate == null
            ? null
            : _dateTimeService.ConvertToUtcTime(model.EndDate.Value, _dateTimeService.CurrentTimeZone).AddDays(1);

        int? orderStatus = model.OrderStatusId > 0 ? model.OrderStatusId : null;
        var paymentStatus = model.PaymentStatusId > 0 ? (PaymentStatus?)model.PaymentStatusId : null;
        var shippingStatus = model.ShippingStatusId > 0 ? (ShippingStatus?)model.ShippingStatusId : null;

        var items = _customerReportService.GetBestCustomersReport(
            model.StoreId,
            createdFromUtc: startDateValue,
            createdToUtc: endDateValue,
            os: orderStatus,
            ps: paymentStatus,
            ss: shippingStatus,
            orderBy: 2,
            pageIndex: pageIndex - 1,
            pageSize: pageSize);

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
                    : _translationService.GetResource("Admin.Customers.Guest");
            report.Add(m);
        }

        return (report, items.TotalCount);
    }
}