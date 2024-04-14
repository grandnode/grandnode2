using Grand.Business.Core.Commands.Checkout.Orders;
using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Catalog.Discounts;
using Grand.Business.Core.Interfaces.Catalog.Prices;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Catalog.Tax;
using Grand.Business.Core.Interfaces.Checkout.GiftVouchers;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Checkout.Payments;
using Grand.Business.Core.Interfaces.Checkout.Shipping;
using Grand.Business.Core.Interfaces.Common.Addresses;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Common.Stores;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Business.Core.Interfaces.Messages;
using Grand.Business.Core.Interfaces.Storage;
using Grand.Business.Core.Queries.Catalog;
using Grand.Domain.Catalog;
using Grand.Domain.Common;
using Grand.Domain.Directory;
using Grand.Domain.Orders;
using Grand.Domain.Payments;
using Grand.Domain.Shipping;
using Grand.Domain.Tax;
using Grand.Infrastructure;
using Grand.Web.Admin.Extensions;
using Grand.Web.Admin.Extensions.Mapping;
using Grand.Web.Admin.Interfaces;
using Grand.Web.Admin.Models.Orders;
using Grand.Web.Common.Extensions;
using MediatR;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Net;
using ProductExtensions = Grand.Domain.Catalog.ProductExtensions;

namespace Grand.Web.Admin.Services;

public class OrderViewModelService : IOrderViewModelService
{
    #region Ctor

    public OrderViewModelService(IOrderService orderService,
        IPricingService priceCalculationService,
        IDateTimeService dateTimeService,
        IPriceFormatter priceFormatter,
        IDiscountService discountService,
        ITranslationService translationService,
        IWorkContext workContext,
        ICurrencyService currencyService,
        IPaymentService paymentService,
        IPaymentTransactionService paymentTransactionService,
        ICountryService countryService,
        IProductService productService,
        IMessageProviderService messageProviderService,
        IProductAttributeService productAttributeService,
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
        IWarehouseService warehouseService,
        IShoppingCartValidator shoppingCartValidator,
        CurrencySettings currencySettings,
        TaxSettings taxSettings,
        AddressSettings addressSettings,
        IOrderTagService orderTagService,
        IOrderStatusService orderStatusService,
        IMediator mediator,
        IProductAttributeFormatter productAttributeFormatter)
    {
        _orderService = orderService;
        _pricingService = priceCalculationService;
        _dateTimeService = dateTimeService;
        _priceFormatter = priceFormatter;
        _discountService = discountService;
        _translationService = translationService;
        _workContext = workContext;
        _currencyService = currencyService;
        _paymentService = paymentService;
        _paymentTransactionService = paymentTransactionService;
        _countryService = countryService;
        _productService = productService;
        _messageProviderService = messageProviderService;
        _productAttributeService = productAttributeService;
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
        _warehouseService = warehouseService;
        _shoppingCartValidator = shoppingCartValidator;
        _currencySettings = currencySettings;
        _taxSettings = taxSettings;
        _addressSettings = addressSettings;
        _customerService = customerService;
        _orderTagService = orderTagService;
        _orderStatusService = orderStatusService;
        _mediator = mediator;
        _productAttributeFormatter = productAttributeFormatter;
    }

    #endregion


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
        var model = new OrderListModel {
            AvailableOrderStatuses = statuses
                .Select(x => new SelectListItem { Value = x.StatusId.ToString(), Text = x.Name }).ToList()
        };
        model.AvailableOrderStatuses.Insert(0,
            new SelectListItem { Text = _translationService.GetResource("Admin.Common.All"), Value = " " });
        if (orderStatusId.HasValue)
        {
            //pre-select value?
            var item = model.AvailableOrderStatuses.FirstOrDefault(x => x.Value == orderStatusId.Value.ToString());
            if (item != null)
                item.Selected = true;
        }

        //payment statuses
        model.AvailablePaymentStatuses =
            PaymentStatus.Pending.ToSelectList(_translationService, _workContext, false).ToList();
        model.AvailablePaymentStatuses.Insert(0,
            new SelectListItem { Text = _translationService.GetResource("Admin.Common.All"), Value = " " });
        if (paymentStatusId.HasValue)
        {
            //pre-select value?
            var item = model.AvailablePaymentStatuses.FirstOrDefault(x =>
                x.Value == paymentStatusId.Value.ToString());
            if (item != null)
                item.Selected = true;
        }

        //order's tags
        model.AvailableOrderTags.Add(new SelectListItem
            { Text = _translationService.GetResource("Admin.Common.All"), Value = " " });
        foreach (var s in await _orderTagService.GetAllOrderTags())
            model.AvailableOrderTags.Add(new SelectListItem { Text = s.Name, Value = s.Id });

        //shipping statuses
        model.AvailableShippingStatuses =
            ShippingStatus.ShippingNotRequired.ToSelectList(_translationService, _workContext, false).ToList();
        model.AvailableShippingStatuses.Insert(0,
            new SelectListItem { Text = _translationService.GetResource("Admin.Common.All"), Value = " " });

        if (shippingStatusId.HasValue)
        {
            //pre-select value?
            var item = model.AvailableShippingStatuses.FirstOrDefault(x =>
                x.Value == shippingStatusId.Value.ToString());
            if (item != null)
                item.Selected = true;
        }

        //stores
        model.AvailableStores.Add(new SelectListItem
            { Text = _translationService.GetResource("Admin.Common.All"), Value = " " });
        foreach (var s in (await _storeService.GetAllStores()).Where(x =>
                     x.Id == storeId || string.IsNullOrWhiteSpace(storeId)))
            model.AvailableStores.Add(new SelectListItem { Text = s.Shortcut, Value = s.Id });

        //vendors
        model.AvailableVendors.Add(new SelectListItem
            { Text = _translationService.GetResource("Admin.Common.All"), Value = " " });
        foreach (var v in await _vendorService.GetAllVendors(showHidden: true))
            model.AvailableVendors.Add(new SelectListItem { Text = v.Name, Value = v.Id });

        //warehouses
        model.AvailableWarehouses.Add(new SelectListItem
            { Text = _translationService.GetResource("Admin.Common.All"), Value = " " });
        foreach (var w in await _warehouseService.GetAllWarehouses())
            model.AvailableWarehouses.Add(new SelectListItem { Text = w.Name, Value = w.Id });

        //payment methods
        model.AvailablePaymentMethods.Add(new SelectListItem
            { Text = _translationService.GetResource("Admin.Common.All"), Value = " " });
        foreach (var pm in _paymentService.LoadAllPaymentMethods())
            model.AvailablePaymentMethods.Add(new SelectListItem { Text = pm.FriendlyName, Value = pm.SystemName });

        //billing countries
        foreach (var c in await _countryService.GetAllCountriesForBilling(showHidden: true))
            model.AvailableCountries.Add(new SelectListItem { Text = c.Name, Value = c.Id });

        model.AvailableCountries.Insert(0,
            new SelectListItem { Text = _translationService.GetResource("Admin.Common.All"), Value = " " });

        if (startDate.HasValue)
            model.StartDate = startDate.Value;

        if (!string.IsNullOrEmpty(code))
            model.GoDirectlyToNumber = code;

        return model;
    }

    public virtual async Task<(IEnumerable<OrderModel> orderModels, int totalCount)> PrepareOrderModel(
        OrderListModel model, int pageIndex, int pageSize)
    {
        DateTime? startDateValue = model.StartDate == null
            ? null
            : _dateTimeService.ConvertToUtcTime(model.StartDate.Value, _dateTimeService.CurrentTimeZone);

        DateTime? endDateValue = model.EndDate == null
            ? null
            : _dateTimeService.ConvertToUtcTime(model.EndDate.Value, _dateTimeService.CurrentTimeZone).AddDays(1);

        int? orderStatus = model.OrderStatusId > 0 ? model.OrderStatusId : null;
        var paymentStatus = model.PaymentStatusId > 0 ? (PaymentStatus?)model.PaymentStatusId : null;
        var shippingStatus =
            model.ShippingStatusId.HasValue ? (ShippingStatus?)model.ShippingStatusId : null;


        var filterByProductId = "";
        var product = await _productService.GetProductById(model.ProductId);
        if (product != null)
            filterByProductId = model.ProductId;

        var salesEmployeeId = _workContext.CurrentCustomer.SeId;

        //load orders
        var orders = await _orderService.SearchOrders(
            model.StoreId,
            model.VendorId,
            model.CustomerId,
            filterByProductId,
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
            var orderTotal = _priceFormatter.FormatPrice(x.OrderTotal,
                await _currencyService.GetCurrencyByCode(x.CustomerCurrencyCode));
            items.Add(new OrderModel {
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
                CustomerFullName = $"{x.BillingAddress?.FirstName} {x.BillingAddress?.LastName}",
                CreatedOn = _dateTimeService.ConvertToUserTime(x.CreatedOnUtc, DateTimeKind.Utc)
            });
        }

        return (items, orders.TotalCount);
    }


    public virtual async Task PrepareOrderDetailsModel(OrderModel model, Order order)
    {
        ArgumentNullException.ThrowIfNull(order);
        ArgumentNullException.ThrowIfNull(model);

        model.Id = order.Id;
        model.OrderNumber = order.OrderNumber;
        model.Code = order.Code;
        model.OrderStatusId = order.OrderStatusId;
        model.OrderGuid = order.OrderGuid;

        var status = await _orderStatusService.GetAll();
        model.OrderStatuses =
            status.Select(x => new SelectListItem { Value = x.StatusId.ToString(), Text = x.Name }).ToList();
        model.OrderStatus = status.FirstOrDefault(x => x.StatusId == order.OrderStatusId)?.Name;

        var store = await _storeService.GetStoreById(order.StoreId);
        model.StoreName = store != null ? store.Shortcut : "Unknown";
        model.CustomerId = order.CustomerId;
        model.UserFields = order.UserFields;

        var customer = await _customerService.GetCustomerById(order.CustomerId);
        if (customer != null)
            model.CustomerInfo = !string.IsNullOrEmpty(customer.Email)
                ? customer.Email
                : _translationService.GetResource("Admin.Customers.Guest");

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

        //order's tags
        if (order.OrderTags.Any())
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

        var primaryStoreCurrency = await _currencyService.GetCurrencyByCode(order.PrimaryCurrencyCode) ??
                                   await _currencyService.GetCurrencyById(_currencySettings.PrimaryStoreCurrencyId);

        if (primaryStoreCurrency == null)
            throw new Exception("Cannot load primary store currency");

        var orderCurrency = await _currencyService.GetCurrencyByCode(order.CustomerCurrencyCode);
        if (orderCurrency == null)
            throw new Exception("Cannot load order currency");

        //subtotal
        model.OrderSubtotalInclTax = _priceFormatter.FormatPrice(order.OrderSubtotalInclTax, orderCurrency);
        model.OrderSubtotalExclTax = _priceFormatter.FormatPrice(order.OrderSubtotalExclTax, orderCurrency);
        model.OrderSubtotalInclTaxValue = order.OrderSubtotalInclTax;
        model.OrderSubtotalExclTaxValue = order.OrderSubtotalExclTax;
        //discount (applied to order subtotal)
        var orderSubtotalDiscountInclTaxStr =
            _priceFormatter.FormatPrice(order.OrderSubTotalDiscountInclTax, orderCurrency);
        var orderSubtotalDiscountExclTaxStr =
            _priceFormatter.FormatPrice(order.OrderSubTotalDiscountExclTax, orderCurrency);
        if (order.OrderSubTotalDiscountInclTax > 0)
            model.OrderSubTotalDiscountInclTax = orderSubtotalDiscountInclTaxStr;
        if (order.OrderSubTotalDiscountExclTax > 0)
            model.OrderSubTotalDiscountExclTax = orderSubtotalDiscountExclTaxStr;
        model.OrderSubTotalDiscountInclTaxValue = order.OrderSubTotalDiscountInclTax;
        model.OrderSubTotalDiscountExclTaxValue = order.OrderSubTotalDiscountExclTax;

        //shipping
        model.OrderShippingInclTax = _priceFormatter.FormatPrice(order.OrderShippingInclTax, orderCurrency);
        model.OrderShippingExclTax = _priceFormatter.FormatPrice(order.OrderShippingExclTax, orderCurrency);
        model.OrderShippingInclTaxValue = order.OrderShippingInclTax;
        model.OrderShippingExclTaxValue = order.OrderShippingExclTax;

        //payment method additional fee
        if (order.PaymentMethodAdditionalFeeInclTax > 0)
        {
            model.PaymentMethodAdditionalFeeInclTax =
                _priceFormatter.FormatPrice(order.PaymentMethodAdditionalFeeInclTax, orderCurrency);
            model.PaymentMethodAdditionalFeeExclTax =
                _priceFormatter.FormatPrice(order.PaymentMethodAdditionalFeeExclTax, orderCurrency);
        }

        model.PaymentMethodAdditionalFeeInclTaxValue = order.PaymentMethodAdditionalFeeInclTax;
        model.PaymentMethodAdditionalFeeExclTaxValue = order.PaymentMethodAdditionalFeeExclTax;

        //tax
        model.Tax = _priceFormatter.FormatPrice(order.OrderTax, orderCurrency);
        var displayTaxRates = _taxSettings.DisplayTaxRates && order.OrderTaxes.Any();
        var displayTax = !displayTaxRates;
        foreach (var tr in order.OrderTaxes)
            model.TaxRates.Add(new OrderModel.TaxRate {
                Rate = _priceFormatter.FormatTaxRate(tr.Percent),
                Value = _priceFormatter.FormatPrice(tr.Amount, orderCurrency)
            });

        model.DisplayTaxRates = displayTaxRates;
        model.DisplayTax = displayTax;
        model.TaxValue = order.OrderTax;
        //model.TaxRatesValue = order.TaxRates;

        //discount
        if (order.OrderDiscount > 0)
            model.OrderTotalDiscount = _priceFormatter.FormatPrice(-order.OrderDiscount, orderCurrency);
        model.OrderTotalDiscountValue = order.OrderDiscount;

        //gift vouchers
        foreach (var gcuh in await _giftVoucherService.GetAllGiftVoucherUsageHistory(order.Id))
        {
            var giftVoucher = await _giftVoucherService.GetGiftVoucherById(gcuh.GiftVoucherId);
            if (giftVoucher != null)
                model.GiftVouchers.Add(new OrderModel.GiftVoucher {
                    CouponCode = giftVoucher.Code,
                    Amount = _priceFormatter.FormatPrice(-gcuh.UsedValue, orderCurrency)
                });
        }

        //loyalty points
        if (order.RedeemedLoyaltyPoints > 0)
        {
            model.RedeemedLoyaltyPoints = order.RedeemedLoyaltyPoints;
            model.RedeemedLoyaltyPointsAmount =
                _priceFormatter.FormatPrice(-order.RedeemedLoyaltyPointsAmount, orderCurrency);
        }

        //total
        model.OrderTotal = _priceFormatter.FormatPrice(order.OrderTotal, orderCurrency);
        model.OrderTotalValue = order.OrderTotal;
        model.CurrencyRate = order.CurrencyRate;
        model.CurrencyCode = order.CustomerCurrencyCode;

        //refunded amount
        if (order.RefundedAmount > 0)
            model.RefundedAmount = _priceFormatter.FormatPrice(order.RefundedAmount, orderCurrency);

        var suggestedRefundedAmount = order.OrderItems.Sum(x => x.CancelAmount);
        if (suggestedRefundedAmount > 0)
            model.SuggestedRefundedAmount = _priceFormatter.FormatPrice(suggestedRefundedAmount, orderCurrency);

        //used discounts
        var duh = await _mediator.Send(new GetDiscountUsageHistoryQuery { OrderId = order.Id });

        foreach (var d in duh)
        {
            var discount = await _discountService.GetDiscountById(d.DiscountId);
            if (discount != null)
                model.UsedDiscounts.Add(new OrderModel.UsedDiscountModel {
                    DiscountId = d.DiscountId,
                    DiscountName = discount.Name
                });
        }

        //profit
        var productCost = order.OrderItems.Sum(orderItem => orderItem.OriginalProductCost * orderItem.Quantity);
        if (order.CurrencyRate > 0)
        {
            var profit = Convert.ToDouble(order.OrderTotal / order.CurrencyRate -
                                          order.OrderShippingExclTax / order.CurrencyRate -
                                          order.OrderTax / order.CurrencyRate - productCost);
            model.Profit = _priceFormatter.FormatPrice(profit, primaryStoreCurrency);
        }

        if (order.PrimaryCurrencyCode != order.CustomerCurrencyCode)
        {
            model.OrderTotal +=
                $" ({_priceFormatter.FormatPrice(order.OrderTotal / order.CurrencyRate, primaryStoreCurrency)})";
            model.OrderSubtotalInclTax +=
                $" ({_priceFormatter.FormatPrice(order.OrderSubtotalInclTax / order.CurrencyRate, primaryStoreCurrency)})";
            model.OrderSubtotalExclTax +=
                $" ({_priceFormatter.FormatPrice(order.OrderSubtotalExclTax / order.CurrencyRate, primaryStoreCurrency)})";

            //discount (applied to order subtotal)
            if (order.OrderSubTotalDiscountInclTax > 0)
                model.OrderSubTotalDiscountInclTax +=
                    $" ({_priceFormatter.FormatPrice(order.OrderSubTotalDiscountInclTax / order.CurrencyRate, primaryStoreCurrency)})";
            if (order.OrderSubTotalDiscountExclTax > 0)
                model.OrderSubTotalDiscountExclTax +=
                    $" ({_priceFormatter.FormatPrice(order.OrderSubTotalDiscountExclTax / order.CurrencyRate, primaryStoreCurrency)})";

            //shipping
            model.OrderShippingInclTax +=
                $" ({_priceFormatter.FormatPrice(order.OrderShippingInclTax / order.CurrencyRate, primaryStoreCurrency)})";
            model.OrderShippingExclTax +=
                $" ({_priceFormatter.FormatPrice(order.OrderShippingExclTax / order.CurrencyRate, primaryStoreCurrency)})";

            //payment method additional fee
            if (order.PaymentMethodAdditionalFeeInclTax > 0)
            {
                model.PaymentMethodAdditionalFeeInclTax +=
                    $" ({_priceFormatter.FormatPrice(order.PaymentMethodAdditionalFeeInclTax / order.CurrencyRate, primaryStoreCurrency)})";
                model.PaymentMethodAdditionalFeeExclTax +=
                    $" ({_priceFormatter.FormatPrice(order.PaymentMethodAdditionalFeeExclTax / order.CurrencyRate, primaryStoreCurrency)})";
            }

            model.Tax +=
                $" ({_priceFormatter.FormatPrice(order.OrderTax / order.CurrencyRate, primaryStoreCurrency)})";

            //refunded amount
            if (order.RefundedAmount > 0)
                model.RefundedAmount +=
                    $" ({_priceFormatter.FormatPrice(order.RefundedAmount / order.CurrencyRate, primaryStoreCurrency)})";
        }

        #endregion

        #region Payment info

        //payment method info
        var pm = _paymentService.LoadPaymentMethodBySystemName(order.PaymentMethodSystemName);
        model.PaymentMethod = pm != null ? pm.FriendlyName : order.PaymentMethodSystemName;
        model.PaymentStatus = order.PaymentStatusId.GetTranslationEnum(_translationService, _workContext);
        model.PaymentStatusEnum = order.PaymentStatusId;
        var pt = await _paymentTransactionService.GetOrderByGuid(order.OrderGuid);
        if (pt != null)
            model.PaymentTransactionId = pt.Id;

        model.PrimaryStoreCurrencyCode = order.PrimaryCurrencyCode;
        model.MaxAmountToRefund = order.OrderTotal - order.RefundedAmount;

        #endregion

        #region Billing & shipping info

        model.BillingAddress = await order.BillingAddress.ToModel(_countryService);
        model.BillingAddress.FormattedCustomAddressAttributes =
            await _addressAttributeParser.FormatAttributes(_workContext.WorkingLanguage,
                order.BillingAddress.Attributes);
        model.BillingAddress.NameEnabled = _addressSettings.NameEnabled;
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
                    model.ShippingAddress.FormattedCustomAddressAttributes =
                        await _addressAttributeParser.FormatAttributes(_workContext.WorkingLanguage,
                            order.ShippingAddress.Attributes);
                    model.ShippingAddress.NameEnabled = _addressSettings.NameEnabled;
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

                    model.ShippingAddressGoogleMapsUrl =
                        $"https://maps.google.com/maps?f=q&hl=en&ie=UTF8&oe=UTF8&geocode=&q={WebUtility.UrlEncode(order.ShippingAddress.Address1 + " " + order.ShippingAddress.ZipPostalCode + " " + order.ShippingAddress.City + " " + (!string.IsNullOrEmpty(order.ShippingAddress.CountryId) ? (await _countryService.GetCountryById(order.ShippingAddress.CountryId))?.Name : ""))}";
                }
            }
            else
            {
                if (order.PickupPoint is { Address: not null })
                {
                    model.PickupAddress = await order.PickupPoint.Address.ToModel(_countryService);
                    var country = await _countryService.GetCountryById(order.PickupPoint.Address.CountryId);
                    if (country != null)
                        model.PickupAddress.CountryName = country.Name;
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
        var hasDownloadableItems = false;
        var products = order.OrderItems;
        foreach (var orderItem in products)
        {
            var product = await _productService.GetProductByIdIncludeArch(orderItem.ProductId);

            if (product == null) continue;

            if (product.IsDownload)
                hasDownloadableItems = true;

            var orderItemModel = new OrderModel.OrderItemModel {
                Id = orderItem.Id,
                ProductId = orderItem.ProductId,
                ProductName = product.Name,
                Sku = orderItem.Sku,
                Quantity = orderItem.Quantity,
                OpenQty = orderItem.OpenQty,
                CancelQty = orderItem.CancelQty,
                ReturnQty = orderItem.ReturnQty,
                ShipQty = orderItem.ShipQty,
                IsDownload = product.IsDownload,
                DownloadCount = orderItem.DownloadCount,
                DownloadActivationType = product.DownloadActivationTypeId,
                IsDownloadActivated = orderItem.IsDownloadActivated
            };
            //picture
            var orderItemPicture =
                await product.GetProductPicture(orderItem.Attributes, _productService, _pictureService);
            orderItemModel.PictureThumbnailUrl = await _pictureService.GetPictureUrl(orderItemPicture, 75);

            //license file
            if (!string.IsNullOrEmpty(orderItem.LicenseDownloadId))
            {
                var licenseDownload = await _downloadService.GetDownloadById(orderItem.LicenseDownloadId);
                if (licenseDownload != null) orderItemModel.LicenseDownloadGuid = licenseDownload.DownloadGuid;
            }

            //vendor
            var vendor = await _vendorService.GetVendorById(orderItem.VendorId);
            orderItemModel.VendorName = vendor != null ? vendor.Name : "";

            //unit price
            orderItemModel.UnitPriceInclTaxValue = orderItem.UnitPriceInclTax;
            orderItemModel.UnitPriceExclTaxValue = orderItem.UnitPriceExclTax;
            orderItemModel.UnitPriceInclTax =
                _priceFormatter.FormatPrice(orderItem.UnitPriceInclTax, orderCurrency);
            orderItemModel.UnitPriceExclTax =
                _priceFormatter.FormatPrice(orderItem.UnitPriceExclTax, orderCurrency);
            //discounts
            orderItemModel.DiscountInclTaxValue = orderItem.DiscountAmountInclTax;
            orderItemModel.DiscountExclTaxValue = orderItem.DiscountAmountExclTax;
            orderItemModel.DiscountInclTax =
                _priceFormatter.FormatPrice(orderItem.DiscountAmountInclTax, orderCurrency);
            orderItemModel.DiscountExclTax =
                _priceFormatter.FormatPrice(orderItem.DiscountAmountExclTax, orderCurrency);
            //subtotal
            orderItemModel.SubTotalInclTaxValue = orderItem.PriceInclTax;
            orderItemModel.SubTotalExclTaxValue = orderItem.PriceExclTax;
            orderItemModel.SubTotalInclTax = _priceFormatter.FormatPrice(orderItem.PriceInclTax, orderCurrency);
            orderItemModel.SubTotalExclTax = _priceFormatter.FormatPrice(orderItem.PriceExclTax, orderCurrency);

            if (order.PrimaryCurrencyCode != order.CustomerCurrencyCode)
            {
                orderItemModel.UnitPriceInclTax +=
                    $" ({_priceFormatter.FormatPrice(orderItem.UnitPriceInclTax / order.CurrencyRate, primaryStoreCurrency)})";
                orderItemModel.UnitPriceExclTax +=
                    $" ({_priceFormatter.FormatPrice(orderItem.UnitPriceExclTax / order.CurrencyRate, primaryStoreCurrency)})";
                orderItemModel.DiscountInclTax +=
                    $" ({_priceFormatter.FormatPrice(orderItem.DiscountAmountInclTax / order.CurrencyRate, primaryStoreCurrency)})";
                orderItemModel.DiscountExclTax +=
                    $" ({_priceFormatter.FormatPrice(orderItem.DiscountAmountExclTax / order.CurrencyRate, primaryStoreCurrency)})";
                orderItemModel.SubTotalInclTax +=
                    $" ({_priceFormatter.FormatPrice(orderItem.PriceInclTax / order.CurrencyRate, primaryStoreCurrency)})";
                orderItemModel.SubTotalExclTax +=
                    $" ({_priceFormatter.FormatPrice(orderItem.PriceExclTax / order.CurrencyRate, primaryStoreCurrency)})";
            }

            // commission
            orderItemModel.CommissionValue = orderItem.Commission;
            orderItemModel.Commission = _priceFormatter.FormatPrice(orderItem.Commission, orderCurrency);

            orderItemModel.AttributeInfo = orderItem.AttributeDescription;
            if (product.IsRecurring)
                orderItemModel.RecurringInfo = string.Format(
                    _translationService.GetResource("Admin.Orders.Products.RecurringPeriod"),
                    product.RecurringCycleLength,
                    product.RecurringCyclePeriodId.GetTranslationEnum(_translationService, _workContext),
                    product.RecurringTotalCycles);

            //merchandise returns
            orderItemModel.MerchandiseReturnIds =
                (await _merchandiseReturnService.SearchMerchandiseReturns(orderItemId: orderItem.Id))
                .Select(rr => rr.Id).ToList();
            //gift vouchers
            orderItemModel.PurchasedGiftVoucherIds =
                (await _giftVoucherService.GetGiftVouchersByPurchasedWithOrderItemId(orderItem.Id))
                .Select(gc => gc.Id).ToList();

            model.Items.Add(orderItemModel);
        }

        model.HasDownloadableProducts = hasDownloadableItems;

        #endregion
    }

    public virtual async Task<OrderModel.AddOrderProductModel> PrepareAddOrderProductModel(Order order)
    {
        var model = new OrderModel.AddOrderProductModel {
            OrderId = order.Id,
            OrderNumber = order.OrderNumber,
            //product types
            AvailableProductTypes = ProductType.SimpleProduct.ToSelectList(_translationService, _workContext, false)
                .ToList()
        };

        model.AvailableProductTypes.Insert(0,
            new SelectListItem { Text = _translationService.GetResource("Admin.Common.All"), Value = "0" });

        return await Task.FromResult(model);
    }

    public virtual async Task<OrderModel.AddOrderProductModel.ProductDetailsModel> PrepareAddProductToOrderModel(
        Order order, string productId)
    {
        var product = await _productService.GetProductById(productId);
        if (product == null)
            throw new ArgumentException("No product found with the specified id");

        var customer = await _customerService.GetCustomerById(order.CustomerId);
        var currency = await _currencyService.GetCurrencyByCode(order.CustomerCurrencyCode);
        var store = await _storeService.GetStoreById(order.StoreId);
        var presetQty = 1;
        var presetPrice = (await _pricingService.GetFinalPrice(product, customer, store, currency, 0, true, presetQty))
            .finalPrice;
        var productPrice = await _taxService.GetProductPrice(product, presetPrice, true, customer);
        var presetPriceInclTax = productPrice.productprice;
        var presetPriceExclTax =
            (await _taxService.GetProductPrice(product, presetPrice, false, customer)).productprice;

        var model = new OrderModel.AddOrderProductModel.ProductDetailsModel {
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
            var productAttribute =
                await _productAttributeService.GetProductAttributeById(attribute.ProductAttributeId);
            var attributeModel = new OrderModel.AddOrderProductModel.ProductAttributeModel {
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
                    var attributeValueModel = new OrderModel.AddOrderProductModel.ProductAttributeValueModel {
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
        if (model.GiftVoucher.IsGiftVoucher) model.GiftVoucher.GiftVoucherType = product.GiftVoucherTypeId;

        return model;
    }

    public virtual async Task<OrderAddressModel> PrepareOrderAddressModel(Order order, Address address)
    {
        var model = new OrderAddressModel {
            OrderId = order.Id,
            Address = await address.ToModel(_countryService)
        };
        model.Address.Id = address.Id;
        model.Address.NameEnabled = _addressSettings.NameEnabled;
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
        model.Address.AddressTypeEnabled = _addressSettings.AddressTypeEnabled;

        //countries
        model.Address.AvailableCountries.Add(new SelectListItem
            { Text = _translationService.GetResource("Admin.Address.SelectCountry"), Value = "" });
        foreach (var c in await _countryService.GetAllCountries(showHidden: true))
            model.Address.AvailableCountries.Add(new SelectListItem
                { Text = c.Name, Value = c.Id, Selected = c.Id == address.CountryId });
        //states
        var states = !string.IsNullOrEmpty(address.CountryId)
            ? (await _countryService.GetCountryById(address.CountryId))?.StateProvinces
            : new List<StateProvince>();
        if (states?.Count > 0)
            foreach (var s in states)
                model.Address.AvailableStates.Add(new SelectListItem
                    { Text = s.Name, Value = s.Id, Selected = s.Id == address.StateProvinceId });

        //customer attribute services
        await model.Address.PrepareCustomAddressAttributes(address, _addressAttributeService,
            _addressAttributeParser);

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
            orderNoteModels.Add(new OrderModel.OrderNote {
                Id = orderNote.Id,
                OrderId = order.Id,
                DownloadId = string.IsNullOrEmpty(orderNote.DownloadId) ? "" : orderNote.DownloadId,
                DownloadGuid = download?.DownloadGuid ?? Guid.Empty,
                DisplayToCustomer = orderNote.DisplayToCustomer,
                Note = orderNote.Note,
                CreatedOn = _dateTimeService.ConvertToUserTime(orderNote.CreatedOnUtc, DateTimeKind.Utc),
                CreatedByCustomer = orderNote.CreatedByCustomer
            });
        }

        return orderNoteModels;
    }

    public virtual async Task InsertOrderNote(Order order, string downloadId, bool displayToCustomer,
        string message)
    {
        var orderNote = new OrderNote {
            DisplayToCustomer = displayToCustomer,
            Note = message,
            DownloadId = downloadId,
            OrderId = order.Id
        };
        await _orderService.InsertOrderNote(orderNote);

        //new order notification
        if (displayToCustomer)
            //email
            await _messageProviderService.SendNewOrderNoteAddedCustomerMessage(order, orderNote);
    }

    public virtual async Task DeleteOrderNote(Order order, string id)
    {
        var orderNote = (await _orderService.GetOrderNotes(order.Id)).FirstOrDefault(on => on.Id == id);
        if (orderNote == null)
            throw new ArgumentException("No order note found with the specified id");

        orderNote.OrderId = order.Id;
        await _orderService.DeleteOrderNote(orderNote);

        //delete an old "attachment" file
        if (!string.IsNullOrEmpty(orderNote.DownloadId))
        {
            var attachment = await _downloadService.GetDownloadById(orderNote.DownloadId);
            if (attachment != null)
                await _downloadService.DeleteDownload(attachment);
        }
    }

    public virtual async Task<Address> UpdateOrderAddress(Order order, Address address, OrderAddressModel model,
        List<CustomAttribute> customAttributes)
    {
        address = model.Address.ToEntity(address);
        address.Attributes = customAttributes;
        await _orderService.UpdateOrder(order);

        //add a note
        await _orderService.InsertOrderNote(new OrderNote {
            Note = "Address has been edited",
            DisplayToCustomer = false,
            OrderId = order.Id
        });
        return address;
    }

    public virtual async Task<IList<string>> AddProductToOrderDetails(AddProductToOrderModel model)
    {
        var order = await _orderService.GetOrderById(model.OrderId);
        var product = await _productService.GetProductById(model.ProductId);
        var customer = await _customerService.GetCustomerById(order.CustomerId);

        var warnings = new List<string>();
        var customattributes = new List<CustomAttribute>();

        #region Product attributes

        var attributes = product.ProductAttributeMappings;
        foreach (var attribute in attributes)
            switch (attribute.AttributeControlTypeId)
            {
                case AttributeControlType.DropdownList:
                case AttributeControlType.RadioList:
                case AttributeControlType.ColorSquares:
                case AttributeControlType.ImageSquares:
                {
                    var ctrlAttributes = model.SelectedAttributes.FirstOrDefault(x => x.Key == attribute.Id)?.Value;
                    if (!string.IsNullOrEmpty(ctrlAttributes))
                        customattributes = ProductExtensions.AddProductAttribute(customattributes,
                            attribute, ctrlAttributes).ToList();
                }
                    break;
                case AttributeControlType.Checkboxes:
                {
                    var cblAttributes = model.SelectedAttributes.FirstOrDefault(x => x.Key == attribute.Id)?.Value;
                    if (!string.IsNullOrEmpty(cblAttributes))
                        foreach (var item in cblAttributes
                                     .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                            customattributes = ProductExtensions.AddProductAttribute(
                                customattributes,
                                attribute, item).ToList();
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
                        customattributes = ProductExtensions.AddProductAttribute(customattributes,
                            attribute, selectedAttributeId).ToList();
                }
                    break;
                case AttributeControlType.TextBox:
                case AttributeControlType.MultilineTextbox:
                {
                    var ctrlAttributes = model.SelectedAttributes.FirstOrDefault(x => x.Key == attribute.Id)?.Value;
                    if (!string.IsNullOrEmpty(ctrlAttributes))
                    {
                        var enteredText = ctrlAttributes.Trim();
                        customattributes = ProductExtensions.AddProductAttribute(customattributes,
                            attribute, enteredText).ToList();
                    }
                }
                    break;
                case AttributeControlType.FileUpload:
                {
                    var guid = model.SelectedAttributes.FirstOrDefault(x => x.Key == attribute.Id)?.Value;
                    Guid.TryParse(guid, out var downloadGuid);
                    var download = await _downloadService.GetDownloadByGuid(downloadGuid);
                    if (download != null)
                        customattributes = ProductExtensions.AddProductAttribute(customattributes,
                            attribute, download.DownloadGuid.ToString()).ToList();
                }
                    break;
            }

        //validate conditional attributes (if specified)
        foreach (var attribute in attributes)
        {
            var conditionMet = product.IsConditionMet(attribute, customattributes);
            if (conditionMet.HasValue && !conditionMet.Value)
                customattributes = ProductExtensions
                    .RemoveProductAttribute(customattributes, attribute).ToList();
        }

        #endregion

        #region Gift vouchers

        var recipientName = model.Giftvoucher?.RecipientName;
        var recipientEmail = model.Giftvoucher?.RecipientEmail;
        var senderName = model.Giftvoucher?.SenderName;
        var senderEmail = model.Giftvoucher?.SenderEmail;
        var giftVoucherMessage = model.Giftvoucher?.Message;
        if (product.IsGiftVoucher)
            customattributes = GiftVoucherExtensions.AddGiftVoucherAttribute(customattributes,
                recipientName, recipientEmail, senderName, senderEmail, giftVoucherMessage).ToList();

        #endregion

        //warnings
        var shoppingCartItem = new ShoppingCartItem {
            ShoppingCartTypeId = ShoppingCartType.ShoppingCart,
            Quantity = model.Quantity,
            WarehouseId = product.WarehouseId,
            Attributes = customattributes
        };

        warnings.AddRange(
            await _shoppingCartValidator.GetShoppingCartItemAttributeWarnings(customer, product, shoppingCartItem));
        warnings.AddRange(
            await _shoppingCartValidator.GetShoppingCartItemGiftVoucherWarnings(customer, product,
                shoppingCartItem));
        if (warnings.Count == 0)
        {
            var attributeDescription =
                await _productAttributeFormatter.FormatAttributes(product, customattributes, customer);

            //save item
            var orderItem = new OrderItem {
                OrderItemGuid = Guid.NewGuid(),
                ProductId = product.Id,
                VendorId = product.VendorId,
                WarehouseId = product.WarehouseId,
                Sku = product.FormatSku(customattributes),
                SeId = order.SeId,
                UnitPriceInclTax = model.UnitPriceExclTax,
                UnitPriceExclTax = model.UnitPriceInclTax,
                PriceInclTax = Math.Round(model.UnitPriceInclTax * model.Quantity, 2),
                PriceExclTax = Math.Round(model.UnitPriceExclTax * model.Quantity, 2),
                TaxRate = model.TaxRate,
                OriginalProductCost = await _pricingService.GetProductCost(product, customattributes),
                AttributeDescription = attributeDescription,
                Attributes = customattributes,
                Quantity = model.Quantity,
                OpenQty = model.Quantity,
                DiscountAmountInclTax = 0,
                DiscountAmountExclTax = 0,
                DownloadCount = 0,
                IsDownloadActivated = false,
                LicenseDownloadId = "",
                IsShipEnabled = product.IsShipEnabled,
                CreatedOnUtc = DateTime.UtcNow
            };

            await _mediator.Send(new InsertOrderItemCommand
                { Order = order, OrderItem = orderItem, Product = product });
        }

        return warnings;
    }

    public virtual async Task<IList<Order>> PrepareOrders(OrderListModel model)
    {
        DateTime? startDateValue = model.StartDate == null
            ? null
            : _dateTimeService.ConvertToUtcTime(model.StartDate.Value, _dateTimeService.CurrentTimeZone);

        DateTime? endDateValue = model.EndDate == null
            ? null
            : _dateTimeService.ConvertToUtcTime(model.EndDate.Value, _dateTimeService.CurrentTimeZone).AddDays(1);

        int? orderStatus = model.OrderStatusId > 0 ? model.OrderStatusId : null;
        var paymentStatus = model.PaymentStatusId > 0 ? (PaymentStatus?)model.PaymentStatusId : null;
        var shippingStatus =
            model.ShippingStatusId > 0 ? (ShippingStatus?)model.ShippingStatusId : null;

        var filterByProductId = "";
        var product = await _productService.GetProductById(model.ProductId);
        if (product != null)
            filterByProductId = model.ProductId;

        var salesEmployeeId = _workContext.CurrentCustomer.SeId;

        //load orders
        var orders = await _orderService.SearchOrders(model.StoreId,
            model.VendorId,
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
    ///     Save order's tag by id
    /// </summary>
    /// <param name="order">Order identifier</param>
    /// <param name="orderTags">Order's tag identifier</param>
    /// <returns>Order's tag</returns>
    public virtual async Task SaveOrderTags(Order order, string orderTags)
    {
        ArgumentNullException.ThrowIfNull(order);

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
            if (ot != null) await _orderTagService.DetachOrderTag(ot.Id, order.Id);
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

            if (!order.OrderTagExists(orderTag)) await _orderTagService.AttachOrderTag(orderTag.Id, order.Id);
        }
    }

    private IList<string> ParseOrderTagsToList(string orderTags)
    {
        var result = new List<string>();
        if (!string.IsNullOrWhiteSpace(orderTags))
        {
            var values = orderTags.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var val1 in values)
                if (!string.IsNullOrEmpty(val1.Trim()))
                    result.Add(val1.Trim());
        }

        return result;
    }

    #region Fields

    private readonly IOrderService _orderService;
    private readonly IPricingService _pricingService;
    private readonly IDateTimeService _dateTimeService;
    private readonly IPriceFormatter _priceFormatter;
    private readonly IDiscountService _discountService;
    private readonly ITranslationService _translationService;
    private readonly IWorkContext _workContext;
    private readonly ICurrencyService _currencyService;
    private readonly IPaymentService _paymentService;
    private readonly IPaymentTransactionService _paymentTransactionService;
    private readonly ICountryService _countryService;
    private readonly IProductService _productService;
    private readonly IMessageProviderService _messageProviderService;
    private readonly IProductAttributeService _productAttributeService;
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
    private readonly IWarehouseService _warehouseService;
    private readonly IProductAttributeFormatter _productAttributeFormatter;
    private readonly IShoppingCartValidator _shoppingCartValidator;
    private readonly CurrencySettings _currencySettings;
    private readonly TaxSettings _taxSettings;
    private readonly AddressSettings _addressSettings;
    private readonly IOrderTagService _orderTagService;
    private readonly IOrderStatusService _orderStatusService;
    private readonly IMediator _mediator;

    #endregion
}