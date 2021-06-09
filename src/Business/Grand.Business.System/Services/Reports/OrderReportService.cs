using Grand.Business.Common.Interfaces.Directory;
using Grand.Business.System.Interfaces.Reports;
using Grand.Business.System.Utilities;
using Grand.Domain;
using Grand.Domain.Catalog;
using Grand.Domain.Data;
using Grand.Domain.Orders;
using Grand.Domain.Payments;
using Grand.Domain.Shipping;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace Grand.Business.System.Services.Reports
{
    /// <summary>
    /// Order report service
    /// </summary>
    public partial class OrderReportService : IOrderReportService
    {
        #region Fields

        private readonly IRepository<Order> _orderRepository;
        private readonly IRepository<ProductAlsoPurchased> _productAlsoPurchasedRepository;
        private readonly IRepository<Product> _productRepository;
        private readonly IDateTimeService _dateTimeService;

        #endregion

        #region Ctor

        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="orderRepository">Order repository</param>
        /// <param name="productAlsoPurchasedRepository">Product also purchased repository</param>
        /// <param name="productRepository">Product repository</param>
        /// <param name="dateTimeService">Datetime helper</param>
        public OrderReportService(IRepository<Order> orderRepository,
            IRepository<ProductAlsoPurchased> productAlsoPurchasedRepository,
            IRepository<Product> productRepository,
            IDateTimeService dateTimeService)
        {
            _orderRepository = orderRepository;
            _productAlsoPurchasedRepository = productAlsoPurchasedRepository;
            _productRepository = productRepository;
            _dateTimeService = dateTimeService;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Get "order by country" report
        /// </summary>
        /// <param name="storeId">Store identifier</param>
        /// <param name="os">Order status</param>
        /// <param name="ps">Payment status</param>
        /// <param name="ss">Shipping status</param>
        /// <param name="startTimeUtc">Start date</param>
        /// <param name="endTimeUtc">End date</param>
        /// <returns>Result</returns>
        public virtual async Task<IList<OrderByCountryReportLine>> GetCountryReport(string storeId, int? os,
            PaymentStatus? ps, ShippingStatus? ss, DateTime? startTimeUtc, DateTime? endTimeUtc)
        {
            var query = from p in _orderRepository.Table
                        select p;

            query = query.Where(o => !o.Deleted);
            if (!String.IsNullOrEmpty(storeId))
                query = query.Where(o => o.StoreId == storeId);
            if (os.HasValue)
                query = query.Where(o => o.OrderStatusId == os.Value);
            if (ps.HasValue)
                query = query.Where(o => o.PaymentStatusId == ps.Value);
            if (ss.HasValue)
                query = query.Where(o => o.ShippingStatusId == ss.Value);
            if (startTimeUtc.HasValue)
                query = query.Where(o => startTimeUtc.Value <= o.CreatedOnUtc);
            if (endTimeUtc.HasValue)
                query = query.Where(o => endTimeUtc.Value >= o.CreatedOnUtc);

            var report = (from oq in query
                          group oq by oq.BillingAddress.CountryId into result
                          select new
                          {
                              CountryId = result.Key,
                              TotalOrders = result.Count(),
                              SumOrders = result.Sum(o => o.OrderTotal / o.CurrencyRate)
                          }
                       )
                       .OrderByDescending(x => x.SumOrders)
                       .Select(r => new OrderByCountryReportLine {
                           CountryId = r.CountryId,
                           TotalOrders = r.TotalOrders,
                           SumOrders = r.SumOrders
                       });

            return await Task.FromResult(report.ToList());
        }


        /// <summary>
        /// Get "order by time" report
        /// </summary>
        /// <param name="storeId">Store identifier; "" to load all records</param>
        /// <param name="startTimeUtc">Start date</param>
        /// <param name="endTimeUtc">End date</param>
        /// <returns>Result</returns>
        public virtual async Task<IList<OrderByTimeReportLine>> GetOrderByTimeReport(string storeId, DateTime? startTimeUtc = null,
            DateTime? endTimeUtc = null)
        {
            List<OrderByTimeReportLine> report = new List<OrderByTimeReportLine>();
            if (!startTimeUtc.HasValue)
                startTimeUtc = DateTime.MinValue;
            if (!endTimeUtc.HasValue)
                endTimeUtc = DateTime.UtcNow;

            var endTime = new DateTime(endTimeUtc.Value.Year, endTimeUtc.Value.Month, endTimeUtc.Value.Day, 23, 59, 00);

            var builderquery = from p in _orderRepository.Table
                               select p;

            builderquery = builderquery.Where(o => !o.Deleted);
            builderquery = builderquery.Where(o => o.CreatedOnUtc >= startTimeUtc.Value && o.CreatedOnUtc <= endTime);

            if (!string.IsNullOrEmpty(storeId))
                builderquery = builderquery.Where(o => o.StoreId == storeId);

            var daydiff = (endTimeUtc.Value - startTimeUtc.Value).TotalDays;
            if (daydiff > 31)
            {
                var query = builderquery.GroupBy(x =>
                    new { Year = x.CreatedOnUtc.Year, Month = x.CreatedOnUtc.Month })
                    .Select(g => new OrderStats {
                        Year = g.Key.Year,
                        Month = g.Key.Month,
                        Count = g.Count(),
                        Amount = g.Sum(x => x.OrderTotal * x.CurrencyRate)
                    }).ToList();
               
                foreach (var item in query)
                {
                    report.Add(new OrderByTimeReportLine() {
                        Time = item.Year.ToString().PadLeft(2, '0') + "-" + item.Month.ToString().PadLeft(2, '0'),
                        SumOrders = Math.Round(item.Amount, 2),
                        TotalOrders = item.Count,
                    });
                }
            }
            else
            {
                var query = builderquery.GroupBy(x =>
                    new { Year = x.CreatedOnUtc.Year, Month = x.CreatedOnUtc.Month, Day = x.CreatedOnUtc.Day })
                    .Select(g => new OrderStats {
                        Year = g.Key.Year,
                        Month = g.Key.Month,
                        Day = g.Key.Day,
                        Count = g.Count(),
                        Amount = g.Sum(x => x.OrderTotal * x.CurrencyRate)
                    }).ToList();

                foreach (var item in query)
                {
                    report.Add(new OrderByTimeReportLine() {
                        Time = item.Year.ToString().PadLeft(2, '0') + "-" + item.Month.ToString().PadLeft(2, '0') + "-" + item.Day.ToString().PadLeft(2, '0'),
                        SumOrders = Math.Round(item.Amount, 2),
                        TotalOrders = item.Count,
                    });
                }
            }
            return await Task.FromResult(report);
        }

        /// <summary>
        /// Get order average report
        /// </summary>
        /// <param name="storeId">Store identifier</param>
        /// <param name="customerId">Customer identifier</param>
        /// <param name="vendorId">Vendor identifier</param>
        /// <param name="salesEmployeeId">Sales employee identifier</param>
        /// <param name="billingCountryId">Billing country identifier</param>
        /// <param name="orderId">Order identifier</param>
        /// <param name="paymentMethodSystemName">Payment method system name; null to load all records</param>
        /// <param name="os">Order status</param>
        /// <param name="ps">Payment status</param>
        /// <param name="ss">Shipping status</param>
        /// <param name="startTimeUtc">Start date</param>
        /// <param name="endTimeUtc">End date</param>
        /// <param name="billingEmail">Billing email. Leave empty to load all records.</param>
        /// <param name="ignoreCancelledOrders">A value indicating whether to ignore cancelled orders</param>
        /// <param name="tagid">Tag ident.</param>
        /// <returns>Result</returns>
        public virtual async Task<OrderAverageReportLine> GetOrderAverageReportLine(string storeId = "", string customerId = "",
            string vendorId = "", string salesEmployeeId = "", string billingCountryId = "",
            string orderId = "", string paymentMethodSystemName = null,
            int? os = null, PaymentStatus? ps = null, ShippingStatus? ss = null,
            DateTime? startTimeUtc = null, DateTime? endTimeUtc = null,
            string billingEmail = null, string billingLastName = "", bool ignoreCancelledOrders = false,
            string tagid = null)
        {
            var builderquery = from p in _orderRepository.Table
                               select p;

            builderquery = builderquery.Where(o => !o.Deleted);
            if (!string.IsNullOrEmpty(storeId))
                builderquery = builderquery.Where(o => o.StoreId == storeId);

            if (!string.IsNullOrEmpty(orderId))
                builderquery = builderquery.Where(o => o.Id == orderId);

            if (!string.IsNullOrEmpty(customerId))
                builderquery = builderquery.Where(o => o.CustomerId == customerId);

            if (!string.IsNullOrEmpty(vendorId))
            {
                builderquery = builderquery
                    .Where(o => o.OrderItems
                    .Any(orderItem => orderItem.VendorId == vendorId));
            }
            if (!string.IsNullOrEmpty(salesEmployeeId))
                builderquery = builderquery.Where(o => o.SeId == salesEmployeeId);

            if (!string.IsNullOrEmpty(billingCountryId))
                builderquery = builderquery.Where(o => o.BillingAddress != null && o.BillingAddress.CountryId == billingCountryId);

            if (ignoreCancelledOrders)
            {
                var cancelledOrderStatusId = OrderStatusSystem.Cancelled;
                builderquery = builderquery.Where(o => o.OrderStatusId != (int)cancelledOrderStatusId);

            }
            if (!string.IsNullOrEmpty(paymentMethodSystemName))
                builderquery = builderquery.Where(o => o.PaymentMethodSystemName == paymentMethodSystemName);

            if (os.HasValue)
                builderquery = builderquery.Where(o => o.OrderStatusId == os.Value);

            if (ps.HasValue)
                builderquery = builderquery.Where(o => o.PaymentStatusId == ps.Value);

            if (ss.HasValue)
                builderquery = builderquery.Where(o => o.ShippingStatusId == ss.Value);

            if (startTimeUtc.HasValue)
                builderquery = builderquery.Where(o => startTimeUtc.Value <= o.CreatedOnUtc);

            if (endTimeUtc.HasValue)
                builderquery = builderquery.Where(o => endTimeUtc.Value >= o.CreatedOnUtc);

            if (!string.IsNullOrEmpty(billingEmail))
                builderquery = builderquery.Where(o => o.BillingAddress != null && !String.IsNullOrEmpty(o.BillingAddress.Email) && o.BillingAddress.Email.Contains(billingEmail));

            if (!string.IsNullOrEmpty(billingLastName))
                builderquery = builderquery.Where(o => o.BillingAddress != null && !String.IsNullOrEmpty(o.BillingAddress.LastName) && o.BillingAddress.LastName.Contains(billingLastName));

            //tag filtering 
            if (!string.IsNullOrEmpty(tagid))
                builderquery = builderquery.Where(o => o.OrderTags.Any(y => y == tagid));

            var query = builderquery
                    .GroupBy(x => 1).Select(g => new OrderAverageReportLine {
                        CountOrders = g.Count(),
                        SumShippingExclTax = g.Sum(o => o.OrderShippingExclTax / o.CurrencyRate),
                        SumTax = g.Sum(o => o.OrderTax / o.CurrencyRate),
                        SumOrders = g.Sum(o => o.OrderTotal / o.CurrencyRate)
                    }).ToList();


            var item2 = query.Count() > 0 ? query.FirstOrDefault() : new OrderAverageReportLine {
                CountOrders = 0,
                SumShippingExclTax = 0,
                SumTax = 0,
                SumOrders = 0,
            };
            return await Task.FromResult(item2);
        }

        /// <summary>
        /// Get order average report
        /// </summary>
        /// <param name="storeId">Store identifier</param>
        /// <param name="os">Order status</param>
        /// <returns>Result</returns>
        public virtual async Task<OrderAverageReportLineSummary> OrderAverageReport(string storeId, int os)
        {
            var item = new OrderAverageReportLineSummary();
            item.OrderStatus = os;

            DateTime nowDt = _dateTimeService.ConvertToUserTime(DateTime.Now);
            TimeZoneInfo timeZone = _dateTimeService.CurrentTimeZone;

            //today
            var t1 = new DateTime(nowDt.Year, nowDt.Month, nowDt.Day);
            if (!timeZone.IsInvalidTime(t1))
            {
                DateTime? startTime1 = _dateTimeService.ConvertToUtcTime(t1, timeZone);
                var todayResult = await GetOrderAverageReportLine(storeId: storeId,
                    os: os,
                    startTimeUtc: startTime1);
                item.SumTodayOrders = todayResult.SumOrders;
                item.CountTodayOrders = todayResult.CountOrders;
            }
            //week
            DayOfWeek fdow = CultureInfo.CurrentCulture.DateTimeFormat.FirstDayOfWeek;
            var today = new DateTime(nowDt.Year, nowDt.Month, nowDt.Day);
            DateTime t2 = today.AddDays(-(today.DayOfWeek - fdow));
            if (!timeZone.IsInvalidTime(t2))
            {
                DateTime? startTime2 = _dateTimeService.ConvertToUtcTime(t2, timeZone);
                var weekResult = await GetOrderAverageReportLine(storeId: storeId,
                    os: os,
                    startTimeUtc: startTime2);
                item.SumThisWeekOrders = weekResult.SumOrders;
                item.CountThisWeekOrders = weekResult.CountOrders;
            }
            //month
            var t3 = new DateTime(nowDt.Year, nowDt.Month, 1);
            if (!timeZone.IsInvalidTime(t3))
            {
                DateTime? startTime3 = _dateTimeService.ConvertToUtcTime(t3, timeZone);
                var monthResult = await GetOrderAverageReportLine(storeId: storeId,
                    os: os,
                    startTimeUtc: startTime3);
                item.SumThisMonthOrders = monthResult.SumOrders;
                item.CountThisMonthOrders = monthResult.CountOrders;
            }
            //year
            var t4 = new DateTime(nowDt.Year, 1, 1);
            if (!timeZone.IsInvalidTime(t4))
            {
                DateTime? startTime4 = _dateTimeService.ConvertToUtcTime(t4, timeZone);
                var yearResult = await GetOrderAverageReportLine(storeId: storeId,
                    os: os,
                    startTimeUtc: startTime4);
                item.SumThisYearOrders = yearResult.SumOrders;
                item.CountThisYearOrders = yearResult.CountOrders;
            }
            //all time
            var allTimeResult = await GetOrderAverageReportLine(storeId: storeId, os: os);
            item.SumAllTimeOrders = allTimeResult.SumOrders;
            item.CountAllTimeOrders = allTimeResult.CountOrders;

            return item;
        }

        /// <summary>
        /// Get best sellers report
        /// </summary>
        /// <param name="storeId">Store identifier</param>
        /// <param name="vendorId">Vendor identifier</param>
        /// <param name="categoryId">Category identifier</param>
        /// <param name="collectionId">Collection identifier</param>
        /// <param name="createdFromUtc">Order created date from (UTC); null to load all records</param>
        /// <param name="createdToUtc">Order created date to (UTC); null to load all records</param>
        /// <param name="os">Order status; null to load all records</param>
        /// <param name="ps">Order payment status; null to load all records</param>
        /// <param name="ss">Shipping status; null to load all records</param>
        /// <param name="billingCountryId">Billing country identifier; "" to load all records</param>
        /// <param name="orderBy">1 - order by quantity, 2 - order by total amount</param>
        /// <param name="pageIndex">Page index</param>
        /// <param name="pageSize">Page size</param>
        /// <param name="showHidden">A value indicating whether to show hidden records</param>
        /// <returns>Result</returns>
        public virtual async Task<IPagedList<BestsellersReportLine>> BestSellersReport(
            string storeId = "", string vendorId = "",
            DateTime? createdFromUtc = null, DateTime? createdToUtc = null,
            int? os = null, PaymentStatus? ps = null, ShippingStatus? ss = null,
            string billingCountryId = "",
            int orderBy = 1,
            int pageIndex = 0, int pageSize = int.MaxValue,
            bool showHidden = false)
        {
            var builderquery = from p in _orderRepository.Table
                               select p;
            
            builderquery = builderquery.Where(o => !o.Deleted);
            
            if (!string.IsNullOrEmpty(storeId))
                builderquery = builderquery.Where(o => o.StoreId == storeId);

            if (!string.IsNullOrEmpty(vendorId))
            {
                builderquery = builderquery
                    .Where(o => o.OrderItems
                    .Any(orderItem => orderItem.VendorId == vendorId));
            }
            if (!string.IsNullOrEmpty(billingCountryId))
                builderquery = builderquery.Where(o => o.BillingAddress != null && o.BillingAddress.CountryId == billingCountryId);


            if (os.HasValue)
                builderquery = builderquery.Where(o => o.OrderStatusId == os.Value);
            if (ps.HasValue)
                builderquery = builderquery.Where(o => o.PaymentStatusId == ps.Value);
            if (ss.HasValue)
                builderquery = builderquery.Where(o => o.ShippingStatusId == ss.Value);
            if (createdFromUtc.HasValue)
                builderquery = builderquery.Where(o => createdFromUtc.Value <= o.CreatedOnUtc);
            if (createdToUtc.HasValue)
                builderquery = builderquery.Where(o => createdToUtc.Value >= o.CreatedOnUtc);

            var query = from p in builderquery
                        from item in p.OrderItems
                        select item;

            if (!string.IsNullOrEmpty(vendorId))
            {
                query = query.Where(x => x.VendorId == vendorId);
            }

            var queryItem = query.GroupBy(x => new { ProductId = x.ProductId }).Select(x => new BestsellersReportLine() {
                ProductId = x.Key.ProductId,
                TotalAmount = x.Sum(y => y.PriceInclTax),
                TotalQuantity = x.Sum(y => y.Quantity)
            });

            var queryItemOrdered = orderBy == 1 ? queryItem.OrderByDescending(x => x.TotalQuantity).ToList() :
                queryItem.OrderByDescending(x => x.TotalAmount).ToList();

            var result = new PagedList<BestsellersReportLine>(queryItemOrdered, pageIndex, pageSize);
            return await Task.FromResult(result);
        }


        /// <summary>
        /// Gets a report of orders in the last days
        /// </summary>
        /// <param name="days">Orders in the last days</param>
        /// <param name="storeId">Store ident</param>
        /// <param name="salesEmployeeId">Sales employee ident</param>
        /// <returns>ReportPeriodOrder</returns>
        public virtual async Task<ReportPeriodOrder> GetOrderPeriodReport(int days, string storeId = "", string salesEmployeeId = "")
        {
            var currentdate = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, DateTime.UtcNow.Day);
            DateTime date = days != 0 ?
                _dateTimeService.ConvertToUtcTime(currentdate, _dateTimeService.CurrentTimeZone).AddDays(-days) :
                _dateTimeService.ConvertToUtcTime(currentdate, _dateTimeService.CurrentTimeZone);

            var query = from o in _orderRepository.Table
                        where !o.Deleted && o.CreatedOnUtc >= date
                        && (string.IsNullOrEmpty(storeId) || o.StoreId == storeId)
                        && (string.IsNullOrEmpty(salesEmployeeId) || o.SeId == salesEmployeeId)
                        group o by 1 into g
                        select new ReportPeriodOrder() { Amount = g.Sum(x => x.OrderTotal / x.CurrencyRate), Count = g.Count() };
            var report = query.ToList()?.FirstOrDefault();
            if (report == null)
                report = new ReportPeriodOrder();
            report.Date = date;
            return await Task.FromResult(report);
        }



        /// <summary>
        /// Gets a list of products (identifiers) purchased by other customers who purchased a specified product
        /// </summary>
        /// <param name="storeId">Store identifier</param>
        /// <param name="productId">Product identifier</param>
        /// <param name="recordsToReturn">Records to return</param>
        /// <param name="showHidden">A value indicating whether to show hidden records</param>
        /// <returns>Products</returns>
        public virtual async Task<string[]> GetAlsoPurchasedProductsIds(string storeId, string productId,
            int recordsToReturn = 5, bool showHidden = false)
        {
            var product = from p in _productAlsoPurchasedRepository.Table
                          where p.ProductId == productId
                          group p by p.ProductId2 into g
                          select new
                          {
                              ProductId = g.Key,
                              ProductsPurchased = g.Sum(x => x.Quantity),
                          };
            product = product.OrderByDescending(x => x.ProductsPurchased);
            if (recordsToReturn > 0)
                product = product.Take(recordsToReturn);

            var report = product.ToList();
            var ids = new List<string>();
            foreach (var reportLine in report)
                ids.Add(reportLine.ProductId);

            return await Task.FromResult(ids.ToArray());
        }

        /// <summary>
        /// Gets a list of products that were never sold
        /// </summary>
        /// <param name="storeId">Store identifier</param>
        /// <param name="vendorId">Vendor identifier</param>
        /// <param name="createdFromUtc">Order created date from (UTC); null to load all records</param>
        /// <param name="createdToUtc">Order created date to (UTC); null to load all records</param>
        /// <param name="pageIndex">Page index</param>
        /// <param name="pageSize">Page size</param>
        /// <param name="showHidden">A value indicating whether to show hidden records</param>
        /// <returns>Products</returns>
        public virtual async Task<IPagedList<Product>> ProductsNeverSold(string storeId = "", string vendorId = "",
            DateTime? createdFromUtc = null, DateTime? createdToUtc = null,
            int pageIndex = 0, int pageSize = int.MaxValue, bool showHidden = false)
        {

            createdFromUtc = !createdFromUtc.HasValue ? DateTime.MinValue : createdFromUtc;
            createdToUtc = !createdToUtc.HasValue ? DateTime.MaxValue : createdToUtc;

            var query = ((from order in _orderRepository.Table
                                where
                                (string.IsNullOrEmpty(storeId) || order.StoreId == storeId) &&
                                (createdFromUtc.Value <= order.CreatedOnUtc) &&
                                (createdToUtc.Value >= order.CreatedOnUtc) &&
                                (!order.Deleted)
                                from orderItem in order.OrderItems
                                select new { orderItem.ProductId }).ToList()).Distinct().Select(x => x.ProductId);

            var qproducts = from p in _productRepository.Table
                            orderby p.Name
                            where (!query.Contains(p.Id)) &&
                                  //include only simple products
                                  (p.ProductTypeId == ProductType.SimpleProduct) &&
                                  (vendorId == "" || p.VendorId == vendorId) &&
                                  (string.IsNullOrEmpty(storeId) || p.Stores.Contains(storeId) || p.LimitedToStores == false) &&
                                  (showHidden || p.Published)
                            select p;

            return await PagedList<Product>.Create(qproducts, pageIndex, pageSize);
        }

        public class UnwindedOrderItem
        {
            public OrderItem OrderItems { get; set; }
        }

        public class OrderStats
        {
            public int Year { get; set; }
            public int Month { get; set; }
            public int Day { get; set; }
            public int Count { get; set; }
            public double Amount { get; set; }
        }

        #endregion
    }
}
