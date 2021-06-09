using Grand.Business.Catalog.Extensions;
using Grand.Business.Catalog.Interfaces.Discounts;
using Grand.Business.Catalog.Interfaces.Prices;
using Grand.Business.Catalog.Interfaces.Products;
using Grand.Business.Catalog.Interfaces.Tax;
using Grand.Business.Checkout.Commands.Models.Orders;
using Grand.Business.Checkout.Extensions;
using Grand.Business.Checkout.Interfaces.GiftVouchers;
using Grand.Business.Checkout.Interfaces.Orders;
using Grand.Business.Checkout.Interfaces.Payments;
using Grand.Business.Checkout.Interfaces.Shipping;
using Grand.Business.Common.Extensions;
using Grand.Business.Common.Interfaces.Addresses;
using Grand.Business.Common.Interfaces.Directory;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Business.Common.Interfaces.Logging;
using Grand.Business.Common.Interfaces.Stores;
using Grand.Business.Customers.Extensions;
using Grand.Business.Customers.Interfaces;
using Grand.Business.Messages.Interfaces;
using Grand.Business.Storage.Interfaces;
using Grand.Business.System.Interfaces.Reports;
using Grand.Domain.Catalog;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Domain.Directory;
using Grand.Domain.Orders;
using Grand.Domain.Payments;
using Grand.Domain.Shipping;
using Grand.Domain.Tax;
using Grand.Infrastructure;
using Grand.Web.Admin.Extensions;
using Grand.Web.Admin.Interfaces;
using Grand.Web.Admin.Models.Orders;
using Grand.Web.Common.Extensions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Grand.Web.Admin.Services
{
    public partial class OrderViewModelService : IOrderViewModelService
    {
        #region Fields

        private readonly IOrderService _orderService;
        private readonly IOrderReportService _orderReportService;
        private readonly IPricingService _pricingService;
        private readonly IDateTimeService _dateTimeService;
        private readonly IPriceFormatter _priceFormatter;
        private readonly IDiscountService _discountService;
        private readonly ITranslationService _translationService;
        private readonly IWorkContext _workContext;
        private readonly IGroupService _groupService;
        private readonly ICurrencyService _currencyService;
        private readonly IPaymentService _paymentService;
        private readonly IPaymentTransactionService _paymentTransactionService;
        private readonly ICountryService _countryService;
        private readonly IProductService _productService;
        private readonly IMessageProviderService _messageProviderService;
        private readonly IProductAttributeService _productAttributeService;
        private readonly IProductAttributeParser _productAttributeParser;
        private readonly IGiftVoucherService _giftVoucherService;
        private readonly IDownloadService _downloadService;
        private readonly IStoreService _storeService;
        private readonly IVendorService _vendorService;
        private readonly ISalesEmployeeService _salesEmployeeService;
        private readonly IAddressAttributeParser _addressAttributeParser;
        private readonly IAddressAttributeService _addressAttributeService;
        private readonly IAffiliateService _affiliateService;
        private readonly IPictureService _pictureService;
        private readonly ITaxService _taxService;
        private readonly IMerchandiseReturnService _merchandiseReturnService;
        private readonly ICustomerService _customerService;
        private readonly ICustomerActivityService _customerActivityService;
        private readonly IWarehouseService _warehouseService;
        private readonly IServiceProvider _serviceProvider;
        private readonly IShoppingCartValidator _shoppingCartValidator;
        private readonly CurrencySettings _currencySettings;
        private readonly TaxSettings _taxSettings;
        private readonly AddressSettings _addressSettings;
        private readonly IOrderTagService _orderTagService;
        private readonly IOrderStatusService _orderStatusService;
        private readonly IMediator _mediator;
        #endregion

        #region Ctor

        public OrderViewModelService(IOrderService orderService,
            IOrderReportService orderReportService,
            IPricingService priceCalculationService,
            IDateTimeService dateTimeService,
            IPriceFormatter priceFormatter,
            IDiscountService discountService,
            ITranslationService translationService,
            IWorkContext workContext,
            IGroupService groupService,
            ICurrencyService currencyService,
            IPaymentService paymentService,
            IPaymentTransactionService paymentTransactionService,
            ICountryService countryService,
            IProductService productService,
            IMessageProviderService messageProviderService,
            IProductAttributeService productAttributeService,
            IProductAttributeParser productAttributeParser,
            IGiftVoucherService giftVoucherService,
            IDownloadService downloadService,
            IStoreService storeService,
            IVendorService vendorService,
            ISalesEmployeeService salesEmployeeService,
            IAddressAttributeParser addressAttributeParser,
            IAddressAttributeService addressAttributeService,
            IAffiliateService affiliateService,
            IPictureService pictureService,
            ITaxService taxService,
            IMerchandiseReturnService merchandiseReturnService,
            ICustomerService customerService,
            ICustomerActivityService customerActivityService,
            IWarehouseService warehouseService,
            IServiceProvider serviceProvider,
            IShoppingCartValidator shoppingCartValidator,
            CurrencySettings currencySettings,
            TaxSettings taxSettings,
            AddressSettings addressSettings,
            IOrderTagService orderTagService,
            IOrderStatusService orderStatusService,
            IMediator mediator)
        {
            _orderService = orderService;
            _orderReportService = orderReportService;
            _pricingService = priceCalculationService;
            _dateTimeService = dateTimeService;
            _priceFormatter = priceFormatter;
            _discountService = discountService;
            _translationService = translationService;
            _workContext = workContext;
            _groupService = groupService;
            _currencyService = currencyService;
            _paymentService = paymentService;
            _paymentTransactionService = paymentTransactionService;
            _countryService = countryService;
            _productService = productService;
            _messageProviderService = messageProviderService;
            _productAttributeService = productAttributeService;
            _productAttributeParser = productAttributeParser;
            _giftVoucherService = giftVoucherService;
            _downloadService = downloadService;
            _storeService = storeService;
            _vendorService = vendorService;
            _salesEmployeeService = salesEmployeeService;
            _addressAttributeParser = addressAttributeParser;
            _addressAttributeService = addressAttributeService;
            _affiliateService = affiliateService;
            _pictureService = pictureService;
            _taxService = taxService;
            _merchandiseReturnService = merchandiseReturnService;
            _customerActivityService = customerActivityService;
            _warehouseService = warehouseService;
            _serviceProvider = serviceProvider;
            _shoppingCartValidator = shoppingCartValidator;
            _currencySettings = currencySettings;
            _taxSettings = taxSettings;
            _addressSettings = addressSettings;
            _customerService = customerService;
            _orderTagService = orderTagService;
            _orderStatusService = orderStatusService;
            _mediator = mediator;
        }

        #endregion

        private IList<string> ParseOrderTagsToList(string orderTags)
        {
            var result = new List<string>();
            if (!string.IsNullOrWhiteSpace(orderTags))
            {
                var values = orderTags.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var val1 in values)
                {
                    if (!string.IsNullOrEmpty(val1.Trim()))
                        result.Add(val1.Trim());
                }
            }
            return result;
        }


        public virtual async Task<OrderListModel> PrepareOrderListModel(
            int? orderStatusId = null,
            int? paymentStatusId = null,
            int? shippingStatusId = null,
            DateTime? startDate = null,
            string storeId = null,
            string code = null)
        {
            //order statuses
            var statuses = await _orderStatusService.GetAll();
            var model = new OrderListModel
            {
                AvailableOrderStatuses = statuses.Select(x => new SelectListItem() { Value = x.StatusId.ToString(), Text = x.Name }).ToList()
            };
            model.AvailableOrderStatuses.Insert(0, new SelectListItem { Text = _translationService.GetResource("Admin.Common.All"), Value = " " });
            if (orderStatusId.HasValue)
            {
                //pre-select value?
                var item = model.AvailableOrderStatuses.FirstOrDefault(x => x.Value == orderStatusId.Value.ToString());
                if (item != null)
                    item.Selected = true;
            }

            //payment statuses
            model.AvailablePaymentStatuses = PaymentStatus.Pending.ToSelectList(_translationService, _workContext, false).ToList();
            model.AvailablePaymentStatuses.Insert(0, new SelectListItem { Text = _translationService.GetResource("Admin.Common.All"), Value = " " });
            if (paymentStatusId.HasValue)
            {
                //pre-select value?
                var item = model.AvailablePaymentStatuses.FirstOrDefault(x => x.Value == paymentStatusId.Value.ToString());
                if (item != null)
                    item.Selected = true;
            }

            //order's tags
            model.AvailableOrderTags.Add(new SelectListItem { Text = _translationService.GetResource("Admin.Common.All"), Value = " " });
            foreach (var s in (await _orderTagService.GetAllOrderTags()))
            {
                model.AvailableOrderTags.Add(new SelectListItem { Text = s.Name, Value = s.Id });
            }

            //shipping statuses
            model.AvailableShippingStatuses = ShippingStatus.Pending.ToSelectList(_translationService, _workContext, false).ToList();
            model.AvailableShippingStatuses.Insert(0, new SelectListItem { Text = _translationService.GetResource("Admin.Common.All"), Value = " " });
            if (shippingStatusId.HasValue)
            {
                //pre-select value?
                var item = model.AvailableShippingStatuses.FirstOrDefault(x => x.Value == shippingStatusId.Value.ToString());
                if (item != null)
                    item.Selected = true;
            }

            //stores
            model.AvailableStores.Add(new SelectListItem { Text = _translationService.GetResource("Admin.Common.All"), Value = " " });
            foreach (var s in (await _storeService.GetAllStores()).Where(x => x.Id == storeId || string.IsNullOrWhiteSpace(storeId)))
                model.AvailableStores.Add(new SelectListItem { Text = s.Shortcut, Value = s.Id.ToString() });

            //vendors
            model.AvailableVendors.Add(new SelectListItem { Text = _translationService.GetResource("Admin.Common.All"), Value = " " });
            foreach (var v in await _vendorService.GetAllVendors(showHidden: true))
                model.AvailableVendors.Add(new SelectListItem { Text = v.Name, Value = v.Id.ToString() });

            //warehouses
            model.AvailableWarehouses.Add(new SelectListItem { Text = _translationService.GetResource("Admin.Common.All"), Value = " " });
            foreach (var w in await _warehouseService.GetAllWarehouses())
                model.AvailableWarehouses.Add(new SelectListItem { Text = w.Name, Value = w.Id.ToString() });

            //payment methods
            model.AvailablePaymentMethods.Add(new SelectListItem { Text = _translationService.GetResource("Admin.Common.All"), Value = " " });
            foreach (var pm in _paymentService.LoadAllPaymentMethods())
                model.AvailablePaymentMethods.Add(new SelectListItem { Text = pm.FriendlyName, Value = pm.SystemName });

            //billing countries
            foreach (var c in await _countryService.GetAllCountriesForBilling(showHidden: true))
            {
                model.AvailableCountries.Add(new SelectListItem { Text = c.Name, Value = c.Id.ToString() });
            }
            model.AvailableCountries.Insert(0, new SelectListItem { Text = _translationService.GetResource("Admin.Common.All"), Value = " " });

            //a vendor should have access only to orders with his products
            model.IsLoggedInAsVendor = _workContext.CurrentVendor != null && !await _groupService.IsStaff(_workContext.CurrentCustomer);
            if (startDate.HasValue)
                model.StartDate = startDate.Value;

            if (!string.IsNullOrEmpty(code))
                model.GoDirectlyToNumber = code;

            return model;
        }
        public virtual async Task<(IEnumerable<OrderModel> orderModels, int totalCount)> PrepareOrderModel(OrderListModel model, int pageIndex, int pageSize)
        {
            DateTime? startDateValue = (model.StartDate == null) ? null
                            : (DateTime?)_dateTimeService.ConvertToUtcTime(model.StartDate.Value, _dateTimeService.CurrentTimeZone);

            DateTime? endDateValue = (model.EndDate == null) ? null
                            : (DateTime?)_dateTimeService.ConvertToUtcTime(model.EndDate.Value, _dateTimeService.CurrentTimeZone).AddDays(1);

            int? orderStatus = model.OrderStatusId > 0 ? model.OrderStatusId : null;
            PaymentStatus? paymentStatus = model.PaymentStatusId > 0 ? (PaymentStatus?)(model.PaymentStatusId) : null;
            ShippingStatus? shippingStatus = model.ShippingStatusId > 0 ? (ShippingStatus?)(model.ShippingStatusId) : null;


            var filterByProductId = "";
            var product = await _productService.GetProductById(model.ProductId);
            if (product != null && _workContext.HasAccessToProduct(product))
                filterByProductId = model.ProductId;

            var salesEmployeeId = _workContext.CurrentCustomer.SeId;

            //load orders
            var orders = await _orderService.SearchOrders(
                storeId: model.StoreId,
                vendorId: model.VendorId,
                customerId: model.CustomerId,
                productId: filterByProductId,
                warehouseId: model.WarehouseId,
                salesEmployeeId: salesEmployeeId,
                paymentMethodSystemName: model.PaymentMethodSystemName,
                createdFromUtc: startDateValue,
                createdToUtc: endDateValue,
                os: orderStatus,
                ps: paymentStatus,
                ss: shippingStatus,
                billingEmail: model.BillingEmail,
                billingLastName: model.BillingLastName,
                billingCountryId: model.BillingCountryId,
                orderGuid: model.OrderGuid,
                orderCode: model.GoDirectlyToNumber,
                pageIndex: pageIndex - 1,
                pageSize: pageSize,
                orderTagId: model.OrderTag);

           
            var primaryStoreCurrency = await _currencyService.GetCurrencyById(_currencySettings.PrimaryStoreCurrencyId);
            if (primaryStoreCurrency == null)
                throw new Exception("Cannot load primary store currency");

            var status = await _orderStatusService.GetAll();
            var items = new List<OrderModel>();
            foreach (var x in orders)
            {
                var store = await _storeService.GetStoreById(x.StoreId);
                var orderTotal = await _priceFormatter.FormatPrice(x.OrderTotal, x.CustomerCurrencyCode, false, _workContext.WorkingLanguage);
                items.Add(new OrderModel
                {
                    Id = x.Id,
                    OrderNumber = x.OrderNumber,
                    Code = x.Code,
                    StoreName = store != null ? store.Shortcut : "Unknown",
                    OrderTotal = orderTotal,
                    CurrencyCode = x.CustomerCurrencyCode,
                    OrderStatus = status.FirstOrDefault(y => y.StatusId == x.OrderStatusId)?.Name,
                    OrderStatusId = x.OrderStatusId,
                    PaymentStatus = x.PaymentStatusId.GetTranslationEnum(_translationService, _workContext),
                    ShippingStatus = x.ShippingStatusId.GetTranslationEnum(_translationService, _workContext),
                    CustomerEmail = x.BillingAddress?.Email,
                    CustomerId = x.CustomerId,
                    CustomerFullName = string.Format("{0} {1}", x.BillingAddress?.FirstName, x.BillingAddress?.LastName),
                    CreatedOn = _dateTimeService.ConvertToUserTime(x.CreatedOnUtc, DateTimeKind.Utc)
                });
            }
            return (items, orders.TotalCount);
        }


        public virtual async Task PrepareOrderDetailsModel(OrderModel model, Order order)
        {
            if (order == null)
                throw new ArgumentNullException(nameof(order));

            if (model == null)
                throw new ArgumentNullException(nameof(model));

            model.Id = order.Id;
            model.OrderNumber = order.OrderNumber;
            model.Code = order.Code;
            model.OrderStatusId = order.OrderStatusId;
            model.OrderGuid = order.OrderGuid;

            var status = await _orderStatusService.GetAll();
            model.OrderStatuses = status.Select(x => new SelectListItem() { Value = x.StatusId.ToString(), Text = x.Name }).ToList();
            model.OrderStatus = status.FirstOrDefault(x => x.StatusId == order.OrderStatusId)?.Name;

            var store = await _storeService.GetStoreById(order.StoreId);
            model.StoreName = store != null ? store.Shortcut : "Unknown";
            model.CustomerId = order.CustomerId;
            model.UserFields = order.UserFields;

            var customer = await _customerService.GetCustomerById(order.CustomerId);
            if (customer != null)
                model.CustomerInfo = !string.IsNullOrEmpty(customer.Email) ? customer.Email : _translationService.GetResource("Admin.Customers.Guest");

            model.CustomerIp = order.CustomerIp;
            model.UrlReferrer = order.UrlReferrer;
            model.VatNumber = order.VatNumber;
            model.CreatedOn = _dateTimeService.ConvertToUserTime(order.CreatedOnUtc, DateTimeKind.Utc);
            model.TaxDisplayType = _taxSettings.TaxDisplayType;

            if (!string.IsNullOrEmpty(order.AffiliateId))
            {
                var affiliate = await _affiliateService.GetAffiliateById(order.AffiliateId);
                if (affiliate != null)
                {
                    model.AffiliateId = affiliate.Id;
                    model.AffiliateName = affiliate.GetFullName();
                }
            }

            if (!string.IsNullOrEmpty(order.SeId))
            {
                var salesEmployee = await _salesEmployeeService.GetSalesEmployeeById(order.SeId);
                if (salesEmployee != null)
                {
                    model.SalesEmployeeId = salesEmployee.Id;
                    model.SalesEmployeeName = salesEmployee.Name;
                }
            }

            //a vendor should have access only to his products
            model.IsLoggedInAsVendor = _workContext.CurrentVendor != null && !await _groupService.IsStaff(_workContext.CurrentCustomer);

            //order's tags
            if (order != null && order.OrderTags.Any())
            {
                var tagsName = new List<string>();
                foreach (var item in order.OrderTags)
                {
                    var tag = await _orderTagService.GetOrderTagById(item);
                    if (tag != null)
                        tagsName.Add(tag.Name);
                }
                model.OrderTags = string.Join(",", tagsName);
            }

            #region Order totals

            var primaryStoreCurrency = await _currencyService.GetCurrencyByCode(order.PrimaryCurrencyCode);

            if (primaryStoreCurrency == null)
                primaryStoreCurrency = await _currencyService.GetCurrencyById(_currencySettings.PrimaryStoreCurrencyId);

            if (primaryStoreCurrency == null)
                throw new Exception("Cannot load primary store currency");

            var orderCurrency = await _currencyService.GetCurrencyByCode(order.CustomerCurrencyCode);
            if (orderCurrency == null)
                throw new Exception("Cannot load order currency");

            //subtotal
            model.OrderSubtotalInclTax = _priceFormatter.FormatPrice(order.OrderSubtotalInclTax, orderCurrency, _workContext.WorkingLanguage, true);
            model.OrderSubtotalExclTax = _priceFormatter.FormatPrice(order.OrderSubtotalExclTax, orderCurrency, _workContext.WorkingLanguage, false);
            model.OrderSubtotalInclTaxValue = order.OrderSubtotalInclTax;
            model.OrderSubtotalExclTaxValue = order.OrderSubtotalExclTax;
            //discount (applied to order subtotal)
            string orderSubtotalDiscountInclTaxStr = _priceFormatter.FormatPrice(order.OrderSubTotalDiscountInclTax, orderCurrency, _workContext.WorkingLanguage, true);
            string orderSubtotalDiscountExclTaxStr = _priceFormatter.FormatPrice(order.OrderSubTotalDiscountExclTax, orderCurrency, _workContext.WorkingLanguage, false);
            if (order.OrderSubTotalDiscountInclTax > 0)
                model.OrderSubTotalDiscountInclTax = orderSubtotalDiscountInclTaxStr;
            if (order.OrderSubTotalDiscountExclTax > 0)
                model.OrderSubTotalDiscountExclTax = orderSubtotalDiscountExclTaxStr;
            model.OrderSubTotalDiscountInclTaxValue = order.OrderSubTotalDiscountInclTax;
            model.OrderSubTotalDiscountExclTaxValue = order.OrderSubTotalDiscountExclTax;

            //shipping
            model.OrderShippingInclTax = _priceFormatter.FormatShippingPrice(order.OrderShippingInclTax, orderCurrency, _workContext.WorkingLanguage, true);
            model.OrderShippingExclTax = _priceFormatter.FormatShippingPrice(order.OrderShippingExclTax, orderCurrency, _workContext.WorkingLanguage, false);
            model.OrderShippingInclTaxValue = order.OrderShippingInclTax;
            model.OrderShippingExclTaxValue = order.OrderShippingExclTax;

            //payment method additional fee
            if (order.PaymentMethodAdditionalFeeInclTax > 0)
            {
                model.PaymentMethodAdditionalFeeInclTax = _priceFormatter.FormatPaymentMethodAdditionalFee(order.PaymentMethodAdditionalFeeInclTax, orderCurrency, _workContext.WorkingLanguage, true);
                model.PaymentMethodAdditionalFeeExclTax = _priceFormatter.FormatPaymentMethodAdditionalFee(order.PaymentMethodAdditionalFeeExclTax, orderCurrency, _workContext.WorkingLanguage, false);
            }
            model.PaymentMethodAdditionalFeeInclTaxValue = order.PaymentMethodAdditionalFeeInclTax;
            model.PaymentMethodAdditionalFeeExclTaxValue = order.PaymentMethodAdditionalFeeExclTax;

            //tax
            model.Tax = await _priceFormatter.FormatPrice(order.OrderTax, order.CustomerCurrencyCode, false, _workContext.WorkingLanguage);
            bool displayTaxRates = _taxSettings.DisplayTaxRates && order.OrderTaxes.Any();
            bool displayTax = !displayTaxRates;
            foreach (var tr in order.OrderTaxes)
            {
                model.TaxRates.Add(new OrderModel.TaxRate
                {
                    Rate = _priceFormatter.FormatTaxRate(tr.Percent),
                    Value = await _priceFormatter.FormatPrice(tr.Amount, order.CustomerCurrencyCode, false, _workContext.WorkingLanguage),
                });
            }
            model.DisplayTaxRates = displayTaxRates;
            model.DisplayTax = displayTax;
            model.TaxValue = order.OrderTax;
            //model.TaxRatesValue = order.TaxRates;

            //discount
            if (order.OrderDiscount > 0)
                model.OrderTotalDiscount = await _priceFormatter.FormatPrice(-order.OrderDiscount, order.CustomerCurrencyCode, false, _workContext.WorkingLanguage);
            model.OrderTotalDiscountValue = order.OrderDiscount;

            //gift vouchers
            foreach (var gcuh in await _giftVoucherService.GetAllGiftVoucherUsageHistory(order.Id))
            {
                var giftVoucher = await _giftVoucherService.GetGiftVoucherById(gcuh.GiftVoucherId);
                if (giftVoucher != null)
                {
                    model.GiftVouchers.Add(new OrderModel.GiftVoucher
                    {
                        CouponCode = giftVoucher.Code,
                        Amount = await _priceFormatter.FormatPrice(-gcuh.UsedValue, order.CustomerCurrencyCode, false, _workContext.WorkingLanguage),
                    });
                }
            }

            //loyalty points
            if (order.RedeemedLoyaltyPoints > 0)
            {
                model.RedeemedLoyaltyPoints = order.RedeemedLoyaltyPoints;
                model.RedeemedLoyaltyPointsAmount = await _priceFormatter.FormatPrice(-order.RedeemedLoyaltyPointsAmount, order.CustomerCurrencyCode, false, _workContext.WorkingLanguage);
            }

            //total
            model.OrderTotal = await _priceFormatter.FormatPrice(order.OrderTotal, order.CustomerCurrencyCode, false, _workContext.WorkingLanguage);
            model.OrderTotalValue = order.OrderTotal;
            model.CurrencyRate = order.CurrencyRate;
            model.CurrencyCode = order.CustomerCurrencyCode;

            //refunded amount
            if (order.RefundedAmount > 0)
                model.RefundedAmount = await _priceFormatter.FormatPrice(order.RefundedAmount, order.CustomerCurrencyCode, false, _workContext.WorkingLanguage);

            var suggestedRefundedAmount = order.OrderItems.Sum(x => x.CancelAmount);
            if (suggestedRefundedAmount > 0)
                model.SuggestedRefundedAmount = await _priceFormatter.FormatPrice(suggestedRefundedAmount, order.CustomerCurrencyCode, false, _workContext.WorkingLanguage);

            //used discounts
            var duh = await _discountService.GetAllDiscountUsageHistory(orderId: order.Id);
            foreach (var d in duh)
            {
                var discount = await _discountService.GetDiscountById(d.DiscountId);
                if (discount != null)
                {
                    model.UsedDiscounts.Add(new OrderModel.UsedDiscountModel
                    {
                        DiscountId = d.DiscountId,
                        DiscountName = discount.Name,
                    });
                }
            }

            //profit (not for vendors)
            if (_workContext.CurrentVendor == null)
            {
                var productCost = order.OrderItems.Sum(orderItem => orderItem.OriginalProductCost * orderItem.Quantity);
                if (order.CurrencyRate > 0)
                {
                    var profit = Convert.ToDouble((order.OrderTotal / order.CurrencyRate) - (order.OrderShippingExclTax / order.CurrencyRate) - (order.OrderTax / order.CurrencyRate) - productCost);
                    model.Profit = await _priceFormatter.FormatPrice(profit, order.PrimaryCurrencyCode, false, _workContext.WorkingLanguage);
                }
            }

            if (order.PrimaryCurrencyCode != order.CustomerCurrencyCode)
            {
                model.OrderTotal += $" ({await _priceFormatter.FormatPrice(order.OrderTotal / order.CurrencyRate, order.PrimaryCurrencyCode, false, _workContext.WorkingLanguage)})";
                model.OrderSubtotalInclTax += $" ({await _priceFormatter.FormatPrice(order.OrderSubtotalInclTax / order.CurrencyRate, order.PrimaryCurrencyCode, _workContext.WorkingLanguage, true)})";
                model.OrderSubtotalExclTax += $" ({await _priceFormatter.FormatPrice(order.OrderSubtotalExclTax / order.CurrencyRate, order.PrimaryCurrencyCode, _workContext.WorkingLanguage, false)})";

                //discount (applied to order subtotal)
                if (order.OrderSubTotalDiscountInclTax > 0)
                    model.OrderSubTotalDiscountInclTax += $" ({await _priceFormatter.FormatPrice(order.OrderSubTotalDiscountInclTax / order.CurrencyRate, order.PrimaryCurrencyCode, _workContext.WorkingLanguage, true)})";
                if (order.OrderSubTotalDiscountExclTax > 0)
                    model.OrderSubTotalDiscountExclTax += $" ({await _priceFormatter.FormatPrice(order.OrderSubTotalDiscountExclTax / order.CurrencyRate, order.PrimaryCurrencyCode, _workContext.WorkingLanguage, false)})";

                //shipping
                model.OrderShippingInclTax += $" ({await _priceFormatter.FormatShippingPrice(order.OrderShippingInclTax / order.CurrencyRate, order.PrimaryCurrencyCode, _workContext.WorkingLanguage, true)})";
                model.OrderShippingExclTax += $" ({await _priceFormatter.FormatShippingPrice(order.OrderShippingExclTax / order.CurrencyRate, order.PrimaryCurrencyCode, _workContext.WorkingLanguage, false)})";

                //payment method additional fee
                if (order.PaymentMethodAdditionalFeeInclTax > 0)
                {
                    model.PaymentMethodAdditionalFeeInclTax += $" ({await _priceFormatter.FormatPaymentMethodAdditionalFee(order.PaymentMethodAdditionalFeeInclTax / order.CurrencyRate, order.PrimaryCurrencyCode, _workContext.WorkingLanguage, true)})";
                    model.PaymentMethodAdditionalFeeExclTax += $" ({await _priceFormatter.FormatPaymentMethodAdditionalFee(order.PaymentMethodAdditionalFeeExclTax / order.CurrencyRate, order.PrimaryCurrencyCode, _workContext.WorkingLanguage, false)})";
                }
                model.Tax += $" ({await _priceFormatter.FormatPrice(order.OrderTax / order.CurrencyRate, order.PrimaryCurrencyCode, false, _workContext.WorkingLanguage)})";

                //refunded amount
                if (order.RefundedAmount > 0)
                    model.RefundedAmount += $" ({await _priceFormatter.FormatPrice(order.RefundedAmount / order.CurrencyRate, order.PrimaryCurrencyCode, false, _workContext.WorkingLanguage)})";
            }

            #endregion

            #region Payment info

            //payment method info
            var pm = _paymentService.LoadPaymentMethodBySystemName(order.PaymentMethodSystemName);
            model.PaymentMethod = pm != null ? pm.FriendlyName : order.PaymentMethodSystemName;
            model.PaymentStatus = order.PaymentStatusId.GetTranslationEnum(_translationService, _workContext);
            model.PaymentStatusEnum = order.PaymentStatusId;
            var pt = await _paymentTransactionService.GetByOrdeGuid(order.OrderGuid);
            if (pt != null)
                model.PaymentTransactionId = pt.Id;

            model.PrimaryStoreCurrencyCode = order.PrimaryCurrencyCode;
            model.MaxAmountToRefund = order.OrderTotal - order.RefundedAmount;

            #endregion

            #region Billing & shipping info

            model.BillingAddress = await order.BillingAddress.ToModel(_countryService);
            model.BillingAddress.FormattedCustomAddressAttributes = await _addressAttributeParser.FormatAttributes(_workContext.WorkingLanguage, order.BillingAddress.Attributes);
            model.BillingAddress.FirstNameEnabled = true;
            model.BillingAddress.FirstNameRequired = true;
            model.BillingAddress.LastNameEnabled = true;
            model.BillingAddress.LastNameRequired = true;
            model.BillingAddress.EmailEnabled = true;
            model.BillingAddress.EmailRequired = true;
            model.BillingAddress.CompanyEnabled = _addressSettings.CompanyEnabled;
            model.BillingAddress.CompanyRequired = _addressSettings.CompanyRequired;
            model.BillingAddress.VatNumberEnabled = _addressSettings.VatNumberEnabled;
            model.BillingAddress.VatNumberRequired = _addressSettings.VatNumberRequired;
            model.BillingAddress.CountryEnabled = _addressSettings.CountryEnabled;
            model.BillingAddress.StateProvinceEnabled = _addressSettings.StateProvinceEnabled;
            model.BillingAddress.CityEnabled = _addressSettings.CityEnabled;
            model.BillingAddress.CityRequired = _addressSettings.CityRequired;
            model.BillingAddress.StreetAddressEnabled = _addressSettings.StreetAddressEnabled;
            model.BillingAddress.StreetAddressRequired = _addressSettings.StreetAddressRequired;
            model.BillingAddress.StreetAddress2Enabled = _addressSettings.StreetAddress2Enabled;
            model.BillingAddress.StreetAddress2Required = _addressSettings.StreetAddress2Required;
            model.BillingAddress.ZipPostalCodeEnabled = _addressSettings.ZipPostalCodeEnabled;
            model.BillingAddress.ZipPostalCodeRequired = _addressSettings.ZipPostalCodeRequired;
            model.BillingAddress.PhoneEnabled = _addressSettings.PhoneEnabled;
            model.BillingAddress.PhoneRequired = _addressSettings.PhoneRequired;
            model.BillingAddress.FaxEnabled = _addressSettings.FaxEnabled;
            model.BillingAddress.FaxRequired = _addressSettings.FaxRequired;
            model.BillingAddress.NoteEnabled = _addressSettings.NoteEnabled;

            model.ShippingStatus = order.ShippingStatusId.GetTranslationEnum(_translationService, _workContext);
            if (order.ShippingStatusId != ShippingStatus.ShippingNotRequired)
            {
                model.IsShippable = true;

                model.PickUpInStore = order.PickUpInStore;
                if (!order.PickUpInStore)
                {
                    if (order.ShippingAddress != null)
                    {
                        model.ShippingAddress = await order.ShippingAddress.ToModel(_countryService);
                        model.ShippingAddress.FormattedCustomAddressAttributes = await _addressAttributeParser.FormatAttributes(_workContext.WorkingLanguage, order.ShippingAddress.Attributes);
                        model.ShippingAddress.FirstNameEnabled = true;
                        model.ShippingAddress.FirstNameRequired = true;
                        model.ShippingAddress.LastNameEnabled = true;
                        model.ShippingAddress.LastNameRequired = true;
                        model.ShippingAddress.EmailEnabled = true;
                        model.ShippingAddress.EmailRequired = true;
                        model.ShippingAddress.CompanyEnabled = _addressSettings.CompanyEnabled;
                        model.ShippingAddress.CompanyRequired = _addressSettings.CompanyRequired;
                        model.ShippingAddress.VatNumberEnabled = _addressSettings.VatNumberEnabled;
                        model.ShippingAddress.VatNumberRequired = _addressSettings.VatNumberRequired;
                        model.ShippingAddress.CountryEnabled = _addressSettings.CountryEnabled;
                        model.ShippingAddress.StateProvinceEnabled = _addressSettings.StateProvinceEnabled;
                        model.ShippingAddress.CityEnabled = _addressSettings.CityEnabled;
                        model.ShippingAddress.CityRequired = _addressSettings.CityRequired;
                        model.ShippingAddress.StreetAddressEnabled = _addressSettings.StreetAddressEnabled;
                        model.ShippingAddress.StreetAddressRequired = _addressSettings.StreetAddressRequired;
                        model.ShippingAddress.StreetAddress2Enabled = _addressSettings.StreetAddress2Enabled;
                        model.ShippingAddress.StreetAddress2Required = _addressSettings.StreetAddress2Required;
                        model.ShippingAddress.ZipPostalCodeEnabled = _addressSettings.ZipPostalCodeEnabled;
                        model.ShippingAddress.ZipPostalCodeRequired = _addressSettings.ZipPostalCodeRequired;
                        model.ShippingAddress.PhoneEnabled = _addressSettings.PhoneEnabled;
                        model.ShippingAddress.PhoneRequired = _addressSettings.PhoneRequired;
                        model.ShippingAddress.FaxEnabled = _addressSettings.FaxEnabled;
                        model.ShippingAddress.FaxRequired = _addressSettings.FaxRequired;
                        model.ShippingAddress.NoteEnabled = _addressSettings.NoteEnabled;

                        model.ShippingAddressGoogleMapsUrl = string.Format("http://maps.google.com/maps?f=q&hl=en&ie=UTF8&oe=UTF8&geocode=&q={0}", WebUtility.UrlEncode(order.ShippingAddress.Address1 + " " + order.ShippingAddress.ZipPostalCode + " " + order.ShippingAddress.City + " " + (!String.IsNullOrEmpty(order.ShippingAddress.CountryId) ? (await _countryService.GetCountryById(order.ShippingAddress.CountryId))?.Name : "")));
                    }
                }
                else
                {
                    if (order.PickupPoint != null)
                    {
                        if (order.PickupPoint.Address != null)
                        {
                            model.PickupAddress = await order.PickupPoint.Address.ToModel(_countryService);
                            var country = await _countryService.GetCountryById(order.PickupPoint.Address.CountryId);
                            if (country != null)
                                model.PickupAddress.CountryName = country.Name;
                        }
                    }
                }
                model.ShippingMethod = order.ShippingMethod;
                model.ShippingAdditionDescription = order.ShippingOptionAttributeDescription;
                model.CanAddNewShipments = false;

                foreach (var orderItem in order.OrderItems)
                {
                    //we can ship only shippable products
                    if (!orderItem.IsShipEnabled)
                        continue;

                    if (orderItem.OpenQty <= 0)
                        continue;

                    model.CanAddNewShipments = true;
                }

            }

            #endregion

            #region Products

            model.CheckoutAttributeInfo = order.CheckoutAttributeDescription;
            bool hasDownloadableItems = false;
            var products = order.OrderItems;
            //a vendor should have access only to his products
            if (_workContext.CurrentVendor != null && !await _groupService.IsStaff(_workContext.CurrentCustomer))
            {
                products = products
                    .Where(orderItem => orderItem.VendorId == _workContext.CurrentVendor.Id)
                    .ToList();
            }
            foreach (var orderItem in products)
            {
                var product = await _productService.GetProductByIdIncludeArch(orderItem.ProductId);

                if (product != null)
                {

                    if (product.IsDownload)
                        hasDownloadableItems = true;

                    var orderItemModel = new OrderModel.OrderItemModel
                    {
                        Id = orderItem.Id,
                        ProductId = orderItem.ProductId,
                        ProductName = product.Name,
                        Sku = product.FormatSku(orderItem.Attributes, _productAttributeParser),
                        Quantity = orderItem.Quantity,
                        OpenQty = orderItem.OpenQty,
                        CancelQty = orderItem.CancelQty,
                        ReturnQty = orderItem.ReturnQty,
                        ShipQty = orderItem.ShipQty,
                        IsDownload = product.IsDownload,
                        DownloadCount = orderItem.DownloadCount,
                        DownloadActivationType = product.DownloadActivationTypeId,
                        IsDownloadActivated = orderItem.IsDownloadActivated,
                    };
                    //picture
                    var orderItemPicture = await product.GetProductPicture(orderItem.Attributes, _productService, _pictureService, _productAttributeParser);
                    orderItemModel.PictureThumbnailUrl = await _pictureService.GetPictureUrl(orderItemPicture, 75, true);

                    //license file
                    if (!string.IsNullOrEmpty(orderItem.LicenseDownloadId))
                    {
                        var licenseDownload = await _downloadService.GetDownloadById(orderItem.LicenseDownloadId);
                        if (licenseDownload != null)
                        {
                            orderItemModel.LicenseDownloadGuid = licenseDownload.DownloadGuid;
                        }
                    }
                    //vendor
                    var vendor = await _vendorService.GetVendorById(orderItem.VendorId);
                    orderItemModel.VendorName = vendor != null ? vendor.Name : "";

                    //unit price
                    orderItemModel.UnitPriceInclTaxValue = orderItem.UnitPriceInclTax;
                    orderItemModel.UnitPriceExclTaxValue = orderItem.UnitPriceExclTax;
                    orderItemModel.UnitPriceInclTax = _priceFormatter.FormatPrice(orderItem.UnitPriceInclTax, orderCurrency, _workContext.WorkingLanguage, true, true);
                    orderItemModel.UnitPriceExclTax = _priceFormatter.FormatPrice(orderItem.UnitPriceExclTax, orderCurrency, _workContext.WorkingLanguage, false, true);
                    //discounts
                    orderItemModel.DiscountInclTaxValue = orderItem.DiscountAmountInclTax;
                    orderItemModel.DiscountExclTaxValue = orderItem.DiscountAmountExclTax;
                    orderItemModel.DiscountInclTax = _priceFormatter.FormatPrice(orderItem.DiscountAmountInclTax, orderCurrency, _workContext.WorkingLanguage, true, true);
                    orderItemModel.DiscountExclTax = _priceFormatter.FormatPrice(orderItem.DiscountAmountExclTax, orderCurrency, _workContext.WorkingLanguage, false, true);
                    //subtotal
                    orderItemModel.SubTotalInclTaxValue = orderItem.PriceInclTax;
                    orderItemModel.SubTotalExclTaxValue = orderItem.PriceExclTax;
                    orderItemModel.SubTotalInclTax = _priceFormatter.FormatPrice(orderItem.PriceInclTax, orderCurrency, _workContext.WorkingLanguage, false, false);
                    orderItemModel.SubTotalExclTax = _priceFormatter.FormatPrice(orderItem.PriceExclTax, orderCurrency, _workContext.WorkingLanguage, false, true);

                    if (order.PrimaryCurrencyCode != order.CustomerCurrencyCode)
                    {
                        orderItemModel.UnitPriceInclTax += $" ({_priceFormatter.FormatPrice(orderItem.UnitPriceInclTax / order.CurrencyRate, primaryStoreCurrency, _workContext.WorkingLanguage, true, true)})";
                        orderItemModel.UnitPriceExclTax += $" ({_priceFormatter.FormatPrice(orderItem.UnitPriceExclTax / order.CurrencyRate, primaryStoreCurrency, _workContext.WorkingLanguage, false, true)})";
                        orderItemModel.DiscountInclTax += $" ({_priceFormatter.FormatPrice(orderItem.DiscountAmountInclTax / order.CurrencyRate, primaryStoreCurrency, _workContext.WorkingLanguage, true, true)})";
                        orderItemModel.DiscountExclTax += $" ({_priceFormatter.FormatPrice(orderItem.DiscountAmountExclTax / order.CurrencyRate, primaryStoreCurrency, _workContext.WorkingLanguage, false, true)})";
                        orderItemModel.SubTotalInclTax += $" ({_priceFormatter.FormatPrice(orderItem.PriceInclTax / order.CurrencyRate, primaryStoreCurrency, _workContext.WorkingLanguage, false, false)})";
                        orderItemModel.SubTotalExclTax += $" ({_priceFormatter.FormatPrice(orderItem.PriceExclTax / order.CurrencyRate, primaryStoreCurrency, _workContext.WorkingLanguage, false, true)})";
                    }

                    // commission
                    orderItemModel.CommissionValue = orderItem.Commission;
                    orderItemModel.Commission = _priceFormatter.FormatPrice(orderItem.Commission, orderCurrency, _workContext.WorkingLanguage, false, false);

                    orderItemModel.AttributeInfo = orderItem.AttributeDescription;
                    if (product.IsRecurring)
                        orderItemModel.RecurringInfo = string.Format(_translationService.GetResource("Admin.Orders.Products.RecurringPeriod"), product.RecurringCycleLength, product.RecurringCyclePeriodId.GetTranslationEnum(_translationService, _workContext));

                    //merchandise returns
                    orderItemModel.MerchandiseReturnIds = (await _merchandiseReturnService.SearchMerchandiseReturns(orderItemId: orderItem.Id))
                        .Select(rr => rr.Id).ToList();
                    //gift vouchers
                    orderItemModel.PurchasedGiftVoucherIds = (await _giftVoucherService.GetGiftVouchersByPurchasedWithOrderItemId(orderItem.Id))
                        .Select(gc => gc.Id).ToList();

                    model.Items.Add(orderItemModel);
                }
            }
            model.HasDownloadableProducts = hasDownloadableItems;
            #endregion
        }

        public virtual async Task<OrderModel.AddOrderProductModel> PrepareAddOrderProductModel(Order order)
        {
            var model = new OrderModel.AddOrderProductModel
            {
                OrderId = order.Id,
                OrderNumber = order.OrderNumber
            };

            //product types
            model.AvailableProductTypes = ProductType.SimpleProduct.ToSelectList(_translationService, _workContext, false).ToList();
            model.AvailableProductTypes.Insert(0, new SelectListItem { Text = _translationService.GetResource("Admin.Common.All"), Value = "0" });

            return await Task.FromResult(model);
        }

        public virtual async Task<OrderModel.AddOrderProductModel.ProductDetailsModel> PrepareAddProductToOrderModel(Order order, string productId)
        {
            var product = await _productService.GetProductById(productId);
            if (product == null)
                throw new ArgumentException("No product found with the specified id");

            var customer = await _customerService.GetCustomerById(order.CustomerId);
            var currency = await _currencyService.GetCurrencyByCode(order.CustomerCurrencyCode);
            var presetQty = 1;
            var presetPrice = (await _pricingService.GetFinalPrice(product, customer, currency, 0, true, presetQty)).finalPrice;
            var productPrice = await _taxService.GetProductPrice(product, presetPrice, true, customer);
            double presetPriceInclTax = productPrice.productprice;
            double presetPriceExclTax = (await _taxService.GetProductPrice(product, presetPrice, false, customer)).productprice;

            var model = new OrderModel.AddOrderProductModel.ProductDetailsModel
            {
                ProductId = product.Id,
                OrderId = order.Id,
                OrderNumber = order.OrderNumber,
                Name = product.Name,
                ProductType = product.ProductTypeId,
                UnitPriceExclTax = presetPriceExclTax,
                UnitPriceInclTax = presetPriceInclTax,
                Quantity = presetQty,
                TaxRate = productPrice.taxRate
            };

            //attributes
            var attributes = product.ProductAttributeMappings;
            foreach (var attribute in attributes)
            {
                var productAttribute = await _productAttributeService.GetProductAttributeById(attribute.ProductAttributeId);
                var attributeModel = new OrderModel.AddOrderProductModel.ProductAttributeModel
                {
                    Id = attribute.Id,
                    ProductAttributeId = attribute.ProductAttributeId,
                    Name = productAttribute.Name,
                    TextPrompt = attribute.TextPrompt,
                    IsRequired = attribute.IsRequired,
                    AttributeControlType = attribute.AttributeControlTypeId
                };

                if (attribute.ShouldHaveValues())
                {
                    //values
                    var attributeValues = attribute.ProductAttributeValues;
                    foreach (var attributeValue in attributeValues)
                    {
                        var attributeValueModel = new OrderModel.AddOrderProductModel.ProductAttributeValueModel
                        {
                            Id = attributeValue.Id,
                            Name = attributeValue.Name,
                            IsPreSelected = attributeValue.IsPreSelected
                        };
                        attributeModel.Values.Add(attributeValueModel);
                    }
                }

                model.ProductAttributes.Add(attributeModel);
            }
            //gift voucher
            model.GiftVoucher.IsGiftVoucher = product.IsGiftVoucher;
            if (model.GiftVoucher.IsGiftVoucher)
            {
                model.GiftVoucher.GiftVoucherType = product.GiftVoucherTypeId;
            }
            return model;
        }
        public virtual async Task<OrderAddressModel> PrepareOrderAddressModel(Order order, Address address)
        {
            var model = new OrderAddressModel
            {
                OrderId = order.Id,
                Address = await address.ToModel(_countryService)
            };
            model.Address.Id = address.Id;
            model.Address.FirstNameEnabled = true;
            model.Address.FirstNameRequired = true;
            model.Address.LastNameEnabled = true;
            model.Address.LastNameRequired = true;
            model.Address.EmailEnabled = true;
            model.Address.EmailRequired = true;
            model.Address.CompanyEnabled = _addressSettings.CompanyEnabled;
            model.Address.CompanyRequired = _addressSettings.CompanyRequired;
            model.Address.VatNumberEnabled = _addressSettings.VatNumberEnabled;
            model.Address.VatNumberRequired = _addressSettings.VatNumberRequired;
            model.Address.CountryEnabled = _addressSettings.CountryEnabled;
            model.Address.StateProvinceEnabled = _addressSettings.StateProvinceEnabled;
            model.Address.CityEnabled = _addressSettings.CityEnabled;
            model.Address.CityRequired = _addressSettings.CityRequired;
            model.Address.StreetAddressEnabled = _addressSettings.StreetAddressEnabled;
            model.Address.StreetAddressRequired = _addressSettings.StreetAddressRequired;
            model.Address.StreetAddress2Enabled = _addressSettings.StreetAddress2Enabled;
            model.Address.StreetAddress2Required = _addressSettings.StreetAddress2Required;
            model.Address.ZipPostalCodeEnabled = _addressSettings.ZipPostalCodeEnabled;
            model.Address.ZipPostalCodeRequired = _addressSettings.ZipPostalCodeRequired;
            model.Address.PhoneEnabled = _addressSettings.PhoneEnabled;
            model.Address.PhoneRequired = _addressSettings.PhoneRequired;
            model.Address.FaxEnabled = _addressSettings.FaxEnabled;
            model.Address.FaxRequired = _addressSettings.FaxRequired;
            model.Address.NoteEnabled = _addressSettings.NoteEnabled;

            //countries
            model.Address.AvailableCountries.Add(new SelectListItem { Text = _translationService.GetResource("Admin.Address.SelectCountry"), Value = "" });
            foreach (var c in await _countryService.GetAllCountries(showHidden: true))
                model.Address.AvailableCountries.Add(new SelectListItem { Text = c.Name, Value = c.Id.ToString(), Selected = (c.Id == address.CountryId) });
            //states
            var states = !String.IsNullOrEmpty(address.CountryId) ? (await _countryService.GetCountryById(address.CountryId))?.StateProvinces : new List<StateProvince>();
            if (states.Count > 0)
            {
                foreach (var s in states)
                    model.Address.AvailableStates.Add(new SelectListItem { Text = s.Name, Value = s.Id.ToString(), Selected = (s.Id == address.StateProvinceId) });
            }

            //customer attribute services
            await model.Address.PrepareCustomAddressAttributes(address, _addressAttributeService, _addressAttributeParser);

            return model;
        }

        public virtual async Task<IList<OrderModel.OrderNote>> PrepareOrderNotes(Order order)
        {
            //order notes
            var orderNoteModels = new List<OrderModel.OrderNote>();
            foreach (var orderNote in (await _orderService.GetOrderNotes(order.Id))
                .OrderByDescending(on => on.CreatedOnUtc))
            {
                var download = await _downloadService.GetDownloadById(orderNote.DownloadId);
                orderNoteModels.Add(new OrderModel.OrderNote
                {
                    Id = orderNote.Id,
                    OrderId = order.Id,
                    DownloadId = String.IsNullOrEmpty(orderNote.DownloadId) ? "" : orderNote.DownloadId,
                    DownloadGuid = download != null ? download.DownloadGuid : Guid.Empty,
                    DisplayToCustomer = orderNote.DisplayToCustomer,
                    Note = orderNote.Note,
                    CreatedOn = _dateTimeService.ConvertToUserTime(orderNote.CreatedOnUtc, DateTimeKind.Utc),
                    CreatedByCustomer = orderNote.CreatedByCustomer
                });
            }
            return orderNoteModels;
        }

        public virtual async Task InsertOrderNote(Order order, string downloadId, bool displayToCustomer, string message)
        {
            var orderNote = new OrderNote
            {
                DisplayToCustomer = displayToCustomer,
                Note = message,
                DownloadId = downloadId,
                OrderId = order.Id,
                CreatedOnUtc = DateTime.UtcNow,
            };
            await _orderService.InsertOrderNote(orderNote);

            //new order notification
            if (displayToCustomer)
            {
                //email
                await _messageProviderService.SendNewOrderNoteAddedCustomerMessage(order, orderNote);
            }
        }

        public virtual async Task DeleteOrderNote(Order order, string id)
        {
            var orderNote = (await _orderService.GetOrderNotes(order.Id)).FirstOrDefault(on => on.Id == id);
            if (orderNote == null)
                throw new ArgumentException("No order note found with the specified id");

            orderNote.OrderId = order.Id;
            await _orderService.DeleteOrderNote(orderNote);
        }

        public virtual async Task LogEditOrder(string orderId)
        {
            await _customerActivityService.InsertActivity("EditOrder", orderId, _translationService.GetResource("ActivityLog.EditOrder"), orderId);
        }
        public virtual async Task<Address> UpdateOrderAddress(Order order, Address address, OrderAddressModel model, List<CustomAttribute> customAttributes)
        {
            address = model.Address.ToEntity(address);
            address.Attributes = customAttributes;
            await _orderService.UpdateOrder(order);

            //add a note
            await _orderService.InsertOrderNote(new OrderNote
            {
                Note = "Address has been edited",
                DisplayToCustomer = false,
                CreatedOnUtc = DateTime.UtcNow,
                OrderId = order.Id,
            });
            await LogEditOrder(order.Id);
            return address;
        }
        public virtual async Task<IList<string>> AddProductToOrderDetails(string orderId, string productId, IFormCollection form)
        {
            var order = await _orderService.GetOrderById(orderId);
            var product = await _productService.GetProductById(productId);
            var customer = await _customerService.GetCustomerById(order.CustomerId);
            //save order item

            //basic properties
            double.TryParse(form["UnitPriceInclTax"], out double unitPriceInclTax);
            double.TryParse(form["UnitPriceExclTax"], out double unitPriceExclTax);
            int.TryParse(form["Quantity"], out int quantity);
            int.TryParse(form["TaxRate"], out int taxRate);

            //attributes
            //warnings
            var warnings = new List<string>();
            var customattributes = new List<CustomAttribute>();

            #region Product attributes

            var attributes = product.ProductAttributeMappings;
            foreach (var attribute in attributes)
            {
                string controlId = string.Format("product_attribute_{0}", attribute.Id);
                switch (attribute.AttributeControlTypeId)
                {
                    case AttributeControlType.DropdownList:
                    case AttributeControlType.RadioList:
                    case AttributeControlType.ColorSquares:
                    case AttributeControlType.ImageSquares:
                        {
                            form.TryGetValue(controlId, out var ctrlAttributes);
                            if (!string.IsNullOrEmpty(ctrlAttributes))
                            {
                                customattributes = _productAttributeParser.AddProductAttribute(customattributes,
                                    attribute, ctrlAttributes).ToList();
                            }
                        }
                        break;
                    case AttributeControlType.Checkboxes:
                        {
                            form.TryGetValue(controlId, out var ctrlAttributes);
                            if (!string.IsNullOrEmpty(ctrlAttributes))
                            {
                                foreach (var item in ctrlAttributes.ToString().Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                                {
                                    customattributes = _productAttributeParser.AddProductAttribute(customattributes,
                                        attribute, item).ToList();
                                }
                            }
                        }
                        break;
                    case AttributeControlType.ReadonlyCheckboxes:
                        {
                            //load read-only (already server-side selected) values
                            var attributeValues = attribute.ProductAttributeValues;
                            foreach (var selectedAttributeId in attributeValues
                                .Where(v => v.IsPreSelected)
                                .Select(v => v.Id)
                                .ToList())
                            {
                                customattributes = _productAttributeParser.AddProductAttribute(customattributes,
                                    attribute, selectedAttributeId.ToString()).ToList();
                            }
                        }
                        break;
                    case AttributeControlType.TextBox:
                    case AttributeControlType.MultilineTextbox:
                        {
                            form.TryGetValue(controlId, out var ctrlAttributes);
                            if (!String.IsNullOrEmpty(ctrlAttributes))
                            {
                                string enteredText = ctrlAttributes.ToString().Trim();
                                customattributes = _productAttributeParser.AddProductAttribute(customattributes,
                                    attribute, enteredText).ToList();
                            }
                        }
                        break;
                    case AttributeControlType.Datepicker:
                        {
                            form.TryGetValue(controlId + "_day", out var day);
                            form.TryGetValue(controlId + "_month", out var month);
                            form.TryGetValue(controlId + "_year", out var year);
                            DateTime? selectedDate = null;
                            try
                            {
                                selectedDate = new DateTime(Int32.Parse(year), Int32.Parse(month), Int32.Parse(day));
                            }
                            catch { }
                            if (selectedDate.HasValue)
                            {
                                customattributes = _productAttributeParser.AddProductAttribute(customattributes,
                                    attribute, selectedDate.Value.ToString("D")).ToList();
                            }
                        }
                        break;
                    case AttributeControlType.FileUpload:
                        {
                            form.TryGetValue(controlId, out var guid);
                            Guid.TryParse(guid, out Guid downloadGuid);
                            var download = await _downloadService.GetDownloadByGuid(downloadGuid);
                            if (download != null)
                            {
                                customattributes = _productAttributeParser.AddProductAttribute(customattributes,
                                        attribute, download.DownloadGuid.ToString()).ToList();
                            }
                        }
                        break;
                    default:
                        break;
                }
            }
            //validate conditional attributes (if specified)
            foreach (var attribute in attributes)
            {
                var conditionMet = _productAttributeParser.IsConditionMet(product, attribute, customattributes);
                if (conditionMet.HasValue && !conditionMet.Value)
                {
                    customattributes = _productAttributeParser.RemoveProductAttribute(customattributes, attribute).ToList();
                }
            }

            #endregion

            #region Gift vouchers

            string recipientName = "";
            string recipientEmail = "";
            string senderName = "";
            string senderEmail = "";
            string giftVoucherMessage = "";
            if (product.IsGiftVoucher)
            {
                foreach (string formKey in form.Keys)
                {
                    if (formKey.Equals("giftvoucher.RecipientName", StringComparison.OrdinalIgnoreCase))
                    {
                        recipientName = form[formKey];
                        continue;
                    }
                    if (formKey.Equals("giftvoucher.RecipientEmail", StringComparison.OrdinalIgnoreCase))
                    {
                        recipientEmail = form[formKey];
                        continue;
                    }
                    if (formKey.Equals("giftvoucher.SenderName", StringComparison.OrdinalIgnoreCase))
                    {
                        senderName = form[formKey];
                        continue;
                    }
                    if (formKey.Equals("giftvoucher.SenderEmail", StringComparison.OrdinalIgnoreCase))
                    {
                        senderEmail = form[formKey];
                        continue;
                    }
                    if (formKey.Equals("giftvoucher.Message", StringComparison.OrdinalIgnoreCase))
                    {
                        giftVoucherMessage = form[formKey];
                        continue;
                    }
                }

                customattributes = _productAttributeParser.AddGiftVoucherAttribute(customattributes,
                    recipientName, recipientEmail, senderName, senderEmail, giftVoucherMessage).ToList();
            }

            #endregion

            //warnings
            var shoppingCartService = _serviceProvider.GetRequiredService<IShoppingCartService>();
            var inventoryManageService = _serviceProvider.GetRequiredService<IInventoryManageService>();

            warnings.AddRange(await _shoppingCartValidator.GetShoppingCartItemAttributeWarnings(customer, product, new ShoppingCartItem()
            {
                ShoppingCartTypeId = ShoppingCartType.ShoppingCart,
                Quantity = quantity,
                WarehouseId = product.WarehouseId,
                Attributes = customattributes
            }));
            warnings.AddRange(_shoppingCartValidator.GetShoppingCartItemGiftVoucherWarnings(ShoppingCartType.ShoppingCart, product, customattributes));
            if (warnings.Count == 0)
            {
                //no errors
                var productAttributeFormatter = _serviceProvider.GetRequiredService<IProductAttributeFormatter>();
                //attributes
                string attributeDescription = await productAttributeFormatter.FormatAttributes(product, customattributes, customer);

                //save item
                var orderItem = new OrderItem
                {
                    OrderItemGuid = Guid.NewGuid(),
                    ProductId = product.Id,
                    VendorId = product.VendorId,
                    WarehouseId = product.WarehouseId,
                    Sku = product.FormatSku(customattributes, _productAttributeParser),
                    SeId = order.SeId,
                    UnitPriceInclTax = unitPriceInclTax,
                    UnitPriceExclTax = unitPriceExclTax,
                    PriceInclTax = Math.Round(unitPriceInclTax * quantity, 2),
                    PriceExclTax = Math.Round(unitPriceExclTax * quantity, 2),
                    TaxRate = taxRate,
                    OriginalProductCost = await _pricingService.GetProductCost(product, customattributes),
                    AttributeDescription = attributeDescription,
                    Attributes = customattributes,
                    Quantity = quantity,
                    OpenQty = quantity,
                    DiscountAmountInclTax = 0,
                    DiscountAmountExclTax = 0,
                    DownloadCount = 0,
                    IsDownloadActivated = false,
                    LicenseDownloadId = "",
                    IsShipEnabled = product.IsShipEnabled,
                    CreatedOnUtc = DateTime.UtcNow,
                };

                await _mediator.Send(new InsertOrderItemCommand() { Order = order, OrderItem = orderItem, Product = product });

                await LogEditOrder(order.Id);

            }
            return warnings;
        }

        public virtual async Task<IList<Order>> PrepareOrders(OrderListModel model)
        {
            DateTime? startDateValue = (model.StartDate == null) ? null
                            : (DateTime?)_dateTimeService.ConvertToUtcTime(model.StartDate.Value, _dateTimeService.CurrentTimeZone);

            DateTime? endDateValue = (model.EndDate == null) ? null
                            : (DateTime?)_dateTimeService.ConvertToUtcTime(model.EndDate.Value, _dateTimeService.CurrentTimeZone).AddDays(1);

            int? orderStatus = model.OrderStatusId > 0 ? model.OrderStatusId : null;
            PaymentStatus? paymentStatus = model.PaymentStatusId > 0 ? (PaymentStatus?)(model.PaymentStatusId) : null;
            ShippingStatus? shippingStatus = model.ShippingStatusId > 0 ? (ShippingStatus?)(model.ShippingStatusId) : null;

            var filterByProductId = "";
            var product = await _productService.GetProductById(model.ProductId);
            if (product != null && _workContext.HasAccessToProduct(product))
                filterByProductId = model.ProductId;

            var salesEmployeeId = _workContext.CurrentCustomer.SeId;

            //load orders
            var orders = await _orderService.SearchOrders(storeId: model.StoreId,
                vendorId: model.VendorId,
                productId: filterByProductId,
                salesEmployeeId: salesEmployeeId,
                warehouseId: model.WarehouseId,
                paymentMethodSystemName: model.PaymentMethodSystemName,
                createdFromUtc: startDateValue,
                createdToUtc: endDateValue,
                os: orderStatus,
                ps: paymentStatus,
                ss: shippingStatus,
                billingEmail: model.BillingEmail,
                billingLastName: model.BillingLastName,
                billingCountryId: model.BillingCountryId,
                orderGuid: model.OrderGuid);

            return orders;
        }

        /// <summary>
        /// Save order's tag by id
        /// </summary>
        /// <param name="order">Order identifier</param>
        /// <param name="orderTags">Order's tag identifier</param>
        /// <returns>Order's tag</returns>
        public virtual async Task SaveOrderTags(Order order, string orderTags)
        {
            if (order == null)
                throw new ArgumentNullException(nameof(order));

            //order's tags
            var existingOrderTags = new List<string>();
            foreach (var item in order.OrderTags)
            {
                var tag = await _orderTagService.GetOrderTagById(item);
                if (tag != null)
                    existingOrderTags.Add(tag.Name);
            }
            var newOrderTags = ParseOrderTagsToList(orderTags);

            // compare 
            var orderTagsToRemove = existingOrderTags.Except(newOrderTags);

            foreach (var orderTag in orderTagsToRemove)
            {
                var ot = await _orderTagService.GetOrderTagByName(orderTag);
                if (ot != null)
                {
                    await _orderTagService.DetachOrderTag(ot.Id, order.Id);
                }
            }

            var allOrderTags = await _orderTagService.GetAllOrderTags();
            foreach (var newOrderTag in newOrderTags)
            {
                OrderTag orderTag;
                var orderTag2 = allOrderTags.ToList().Find(o => o.Name == newOrderTag);

                if (orderTag2 == null)
                {
                    orderTag = new OrderTag { Name = newOrderTag, Count = 0 };
                    await _orderTagService.InsertOrderTag(orderTag);
                }
                else
                {
                    orderTag = orderTag2;
                }

                if (!order.OrderTagExists(orderTag))
                {
                    await _orderTagService.AttachOrderTag(orderTag.Id, order.Id);
                }
            }
        }

    }
}
