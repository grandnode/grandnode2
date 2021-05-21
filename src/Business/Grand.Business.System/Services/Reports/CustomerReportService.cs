using Grand.Business.Common.Interfaces.Directory;
using Grand.Business.Customers.Interfaces;
using Grand.Business.System.Interfaces.Reports;
using Grand.Business.System.Utilities;
using Grand.Domain;
using Grand.Domain.Customers;
using Grand.Domain.Data;
using Grand.Domain.Orders;
using Grand.Domain.Payments;
using Grand.Domain.Shipping;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Grand.Business.System.Services.Reports
{
    /// <summary>
    /// Customer report service
    /// </summary>
    public partial class CustomerReportService : ICustomerReportService
    {
        #region Fields

        private readonly IRepository<Customer> _customerRepository;
        private readonly IRepository<Order> _orderRepository;
        private readonly ICustomerService _customerService;
        private readonly IGroupService _groupService;
        private readonly IDateTimeService _dateTimeService;

        #endregion

        #region Ctor

        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="customerRepository">Customer repository</param>
        /// <param name="orderRepository">Order repository</param>
        /// <param name="customerService">Customer service</param>
        /// <param name="dateTimeService">Date time helper</param>
        /// <param name="groupService">group service</param>
        public CustomerReportService(IRepository<Customer> customerRepository,
            IRepository<Order> orderRepository,
            ICustomerService customerService,
            IGroupService groupService,
            IDateTimeService dateTimeService)
        {
            _customerRepository = customerRepository;
            _orderRepository = orderRepository;
            _customerService = customerService;
            _groupService = groupService;
            _dateTimeService = dateTimeService;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Get best customers
        /// </summary>
        /// <param name="storeId">Store ident</param>
        /// <param name="createdFromUtc">Order created date from (UTC); null to load all records</param>
        /// <param name="createdToUtc">Order created date to (UTC); null to load all records</param>
        /// <param name="os">Order status; null to load all records</param>
        /// <param name="ps">Order payment status; null to load all records</param>
        /// <param name="ss">Order shipment status; null to load all records</param>
        /// <param name="orderBy">1 - order by order total, 2 - order by number of orders</param>
        /// <param name="pageIndex">Page index</param>
        /// <param name="pageSize">Page size</param>
        /// <returns>Report</returns>
        public virtual IPagedList<BestCustomerReportLine> GetBestCustomersReport(string storeId, DateTime? createdFromUtc,
            DateTime? createdToUtc, int? os, PaymentStatus? ps, ShippingStatus? ss, int orderBy,
            int pageIndex = 0, int pageSize = 214748364)
        {

            var query = from p in _orderRepository.Table
                        select p;

            query = query.Where(o => !o.Deleted);
            if (os.HasValue)
                query = query.Where(o => o.OrderStatusId == os.Value);
            if (ps.HasValue)
                query = query.Where(o => o.PaymentStatusId == ps.Value);
            if (ss.HasValue)
                query = query.Where(o => o.ShippingStatusId == ss.Value);
            if (createdFromUtc.HasValue)
                query = query.Where(o => createdFromUtc.Value <= o.CreatedOnUtc);
            if (createdToUtc.HasValue)
                query = query.Where(o => createdToUtc.Value >= o.CreatedOnUtc);
            if (!string.IsNullOrEmpty(storeId))
                query = query.Where(o => o.StoreId == storeId);

            var query2 = from co in query
                         group co by co.CustomerId into g
                         select new
                         {
                             CustomerId = g.Key,
                             OrderTotal = g.Sum(x => x.OrderTotal / x.CurrencyRate),
                             OrderCount = g.Count()
                         };
            switch (orderBy)
            {
                case 1:
                    {
                        query2 = query2.OrderByDescending(x => x.OrderTotal);
                    }
                    break;
                case 2:
                    {
                        query2 = query2.OrderByDescending(x => x.OrderCount);
                    }
                    break;
                default:
                    throw new ArgumentException("Wrong orderBy parameter", "orderBy");
            }

            var tmp = new PagedList<dynamic>(query2, pageIndex, pageSize);
            return new PagedList<BestCustomerReportLine>(tmp.Select(x => new BestCustomerReportLine {
                CustomerId = x.CustomerId,
                OrderTotal = x.OrderTotal,
                OrderCount = x.OrderCount
            }),
                tmp.PageIndex, tmp.PageSize, tmp.TotalCount);
        }

        /// <summary>
        /// Gets a report of customers registered in the last days
        /// </summary>
        /// <param name="storeId">Store ident</param>
        /// <param name="days">Customers registered in the last days</param>
        /// <returns>Number of registered customers</returns>
        public virtual async Task<int> GetRegisteredCustomersReport(string storeId, int days)
        {
            DateTime date = _dateTimeService.ConvertToUserTime(DateTime.Now).AddDays(-days);

            var registeredCustomerGroup = await _groupService.GetCustomerGroupBySystemName(SystemCustomerGroupNames.Registered);
            if (registeredCustomerGroup == null)
                return 0;

            var query = from c in _customerRepository.Table
                        where !c.Deleted &&
                        (string.IsNullOrEmpty(storeId) || c.StoreId == storeId) &&
                        c.Groups.Any(cr => cr == registeredCustomerGroup.Id) &&
                        c.CreatedOnUtc >= date
                        //&& c.CreatedOnUtc <= DateTime.UtcNow
                        select c;
            int count = query.Count();
            return count;
        }


        /// <summary>
        /// Get "customer by time" report
        /// </summary>
        /// <param name="startTimeUtc">Start date</param>
        /// <param name="endTimeUtc">End date</param>
        /// <returns>Result</returns>
        public virtual async Task<IList<CustomerByTimeReportLine>> GetCustomerByTimeReport(string storeId, DateTime? startTimeUtc = null,
            DateTime? endTimeUtc = null)

        {
            List<CustomerByTimeReportLine> report = new List<CustomerByTimeReportLine>();
            if (!startTimeUtc.HasValue)
                startTimeUtc = DateTime.MinValue;
            if (!endTimeUtc.HasValue)
                endTimeUtc = DateTime.UtcNow;

            var endTime = new DateTime(endTimeUtc.Value.Year, endTimeUtc.Value.Month, endTimeUtc.Value.Day, 23, 59, 00);
            var builderquery = from p in _customerRepository.Table
                               select p;

            var customergroup = await _groupService.GetCustomerGroupBySystemName(SystemCustomerGroupNames.Registered);
            var customerGroupRegister = customergroup.Id;
            builderquery = builderquery.Where(o => !o.Deleted);
            builderquery = builderquery.Where(o => o.CreatedOnUtc >= startTimeUtc.Value && o.CreatedOnUtc <= endTime);
            builderquery = builderquery.Where(o => o.Groups.Any(y => y == customerGroupRegister));
            if (!string.IsNullOrEmpty(storeId))
                builderquery = builderquery.Where(o => o.StoreId == storeId);

            var daydiff = (endTimeUtc.Value - startTimeUtc.Value).TotalDays;
            if (daydiff > 31)
            {
                var query = builderquery.GroupBy(x => new
                { Year = x.CreatedOnUtc.Year, Month = x.CreatedOnUtc.Month })
                    .Select(g => new CustomerStats {
                        Year = g.Key.Year,
                        Month = g.Key.Month,
                        Count = g.Count(),
                    }).ToList();
                foreach (var item in query)
                {
                    report.Add(new CustomerByTimeReportLine() {
                        Time = item.Year.ToString() + "-" + item.Month.ToString().PadLeft(2, '0'),
                        Registered = item.Count,
                    });
                }
            }
            else
            {
                var query = builderquery.GroupBy(x =>
                    new { Year = x.CreatedOnUtc.Year, Month = x.CreatedOnUtc.Month, Day = x.CreatedOnUtc.Day })
                    .Select(g => new CustomerStats {
                        Year = g.Key.Year,
                        Month = g.Key.Month,
                        Day = g.Key.Day,
                        Count = g.Count(),
                    }).ToList();
                foreach (var item in query)
                {
                    report.Add(new CustomerByTimeReportLine() {
                        Time = item.Year.ToString() + "-" + item.Month.ToString().PadLeft(2, '0') + "-" + item.Day.ToString().PadLeft(2, '0'),
                        Registered = item.Count,
                    });
                }
            }



            return report.OrderBy(x => x.Time).ToList();
        }

        #endregion
    }

    public class CustomerStats
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public int Day { get; set; }
        public int Count { get; set; }
    }
}