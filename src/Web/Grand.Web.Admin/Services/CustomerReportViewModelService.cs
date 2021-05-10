using Grand.Infrastructure;
using Grand.Domain.Customers;
using Grand.Domain.Orders;
using Grand.Domain.Payments;
using Grand.Domain.Shipping;
using Grand.Web.Common.Extensions;
using Grand.Business.Catalog.Interfaces.Categories;
using Grand.Business.Customers.Interfaces;
using Grand.Business.Common.Interfaces.Directory;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Web.Admin.Interfaces;
using Grand.Web.Admin.Models.Customers;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Grand.Business.System.Interfaces.Reports;
using Grand.Business.Catalog.Interfaces.Prices;
using Grand.Business.Checkout.Interfaces.Orders;

namespace Grand.Web.Admin.Services
{
    public class CustomerReportViewModelService : ICustomerReportViewModelService
    {
        private readonly IWorkContext _workContext;
        private readonly ICustomerService _customerService;
        private readonly ITranslationService _translationService;
        private readonly ICustomerReportService _customerReportService;
        private readonly IDateTimeService _dateTimeService;
        private readonly IPriceFormatter _priceFormatter;
        private readonly IOrderStatusService _orderStatusService;

        public CustomerReportViewModelService(IWorkContext workContext,
            ICustomerService customerService,
            ITranslationService translationService, ICustomerReportService customerReportService,
            IDateTimeService dateTimeService, IPriceFormatter priceFormatter,
            IOrderStatusService orderStatusService)
        {
            _workContext = workContext;
            _customerService = customerService;
            _translationService = translationService;
            _customerReportService = customerReportService;
            _dateTimeService = dateTimeService;
            _priceFormatter = priceFormatter;
            _orderStatusService = orderStatusService;
        }

        public virtual async Task<CustomerReportsModel> PrepareCustomerReportsModel()
        {
            var model = new CustomerReportsModel {
                //customers by number of orders
                BestCustomersByNumberOfOrders = new BestCustomersReportModel()
            };
            var status = await _orderStatusService.GetAll();

            model.BestCustomersByNumberOfOrders.AvailableOrderStatuses = status.Select(x => new SelectListItem() { Value = x.StatusId.ToString(), Text = x.Name }).ToList();

            model.BestCustomersByNumberOfOrders.AvailableOrderStatuses.Insert(0, new SelectListItem { Text = _translationService.GetResource("Admin.Common.All"), Value = "" });
            model.BestCustomersByNumberOfOrders.AvailablePaymentStatuses = PaymentStatus.Pending.ToSelectList(_translationService, _workContext, false).ToList();
            model.BestCustomersByNumberOfOrders.AvailablePaymentStatuses.Insert(0, new SelectListItem { Text = _translationService.GetResource("Admin.Common.All"), Value = "" });
            model.BestCustomersByNumberOfOrders.AvailableShippingStatuses = ShippingStatus.Pending.ToSelectList(_translationService, _workContext, false).ToList();
            model.BestCustomersByNumberOfOrders.AvailableShippingStatuses.Insert(0, new SelectListItem { Text = _translationService.GetResource("Admin.Common.All"), Value = "" });

            //customers by order total
            model.BestCustomersByOrderTotal = new BestCustomersReportModel {
                AvailableOrderStatuses = status.Select(x => new SelectListItem() { Value = x.StatusId.ToString(), Text = x.Name }).ToList()
            };
            model.BestCustomersByOrderTotal.AvailableOrderStatuses.Insert(0, new SelectListItem { Text = _translationService.GetResource("Admin.Common.All"), Value = "" });
            model.BestCustomersByOrderTotal.AvailablePaymentStatuses = PaymentStatus.Pending.ToSelectList(_translationService, _workContext, false).ToList();
            model.BestCustomersByOrderTotal.AvailablePaymentStatuses.Insert(0, new SelectListItem { Text = _translationService.GetResource("Admin.Common.All"), Value = "" });
            model.BestCustomersByOrderTotal.AvailableShippingStatuses = ShippingStatus.Pending.ToSelectList(_translationService, _workContext, false).ToList();
            model.BestCustomersByOrderTotal.AvailableShippingStatuses.Insert(0, new SelectListItem { Text = _translationService.GetResource("Admin.Common.All"), Value = "" });

            return model;
        }

        public virtual async Task<IList<RegisteredCustomerReportLineModel>> GetReportRegisteredCustomersModel(string storeId)
        {
            var report = new List<RegisteredCustomerReportLineModel>
            {
                new RegisteredCustomerReportLineModel
                {
                    Period = _translationService.GetResource("Admin.Reports.Customers.RegisteredCustomers.Fields.Period.7days"),
                    Customers = await _customerReportService.GetRegisteredCustomersReport(storeId, 7)
                },

                new RegisteredCustomerReportLineModel
                {
                    Period = _translationService.GetResource("Admin.Reports.Customers.RegisteredCustomers.Fields.Period.14days"),
                    Customers = await _customerReportService.GetRegisteredCustomersReport(storeId, 14)
                },
                new RegisteredCustomerReportLineModel
                {
                    Period = _translationService.GetResource("Admin.Reports.Customers.RegisteredCustomers.Fields.Period.month"),
                    Customers = await _customerReportService.GetRegisteredCustomersReport(storeId, 30)
                },
                new RegisteredCustomerReportLineModel
                {
                    Period = _translationService.GetResource("Admin.Reports.Customers.RegisteredCustomers.Fields.Period.year"),
                    Customers = await _customerReportService.GetRegisteredCustomersReport(storeId, 365)
                }
            };

            return report;
        }

        public virtual async Task<(IEnumerable<BestCustomerReportLineModel> bestCustomerReportLineModels, int totalCount)> PrepareBestCustomerReportLineModel(BestCustomersReportModel model, int orderBy, int pageIndex, int pageSize)
        {
            DateTime? startDateValue = (model.StartDate == null) ? null
                            : (DateTime?)_dateTimeService.ConvertToUtcTime(model.StartDate.Value, _dateTimeService.CurrentTimeZone);

            DateTime? endDateValue = (model.EndDate == null) ? null
                            : (DateTime?)_dateTimeService.ConvertToUtcTime(model.EndDate.Value, _dateTimeService.CurrentTimeZone).AddDays(1);

            int? orderStatus = model.OrderStatusId > 0 ? model.OrderStatusId : null;
            PaymentStatus? paymentStatus = model.PaymentStatusId > 0 ? (PaymentStatus?)(model.PaymentStatusId) : null;
            ShippingStatus? shippingStatus = model.ShippingStatusId > 0 ? (ShippingStatus?)(model.ShippingStatusId) : null;

            var items = _customerReportService.GetBestCustomersReport(model.StoreId, startDateValue, endDateValue,
                orderStatus, paymentStatus, shippingStatus, 2, pageIndex - 1, pageSize);

            var report = new List<BestCustomerReportLineModel>();
            foreach (var x in items)
            {
                var m = new BestCustomerReportLineModel {
                    CustomerId = x.CustomerId,
                    OrderTotal = _priceFormatter.FormatPrice(x.OrderTotal, false),
                    OrderCount = x.OrderCount,
                };
                var customer = await _customerService.GetCustomerById(x.CustomerId);
                if (customer != null)
                {
                    m.CustomerName = !string.IsNullOrEmpty(customer.Email) ? customer.Email : _translationService.GetResource("Admin.Customers.Guest");
                }
                report.Add(m);
            }
            return (report, items.TotalCount);
        }
    }
}
