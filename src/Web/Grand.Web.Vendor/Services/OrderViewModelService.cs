using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Catalog.Prices;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Checkout.GiftVouchers;
using Grand.Business.Core.Interfaces.Checkout.Orders;
using Grand.Business.Core.Interfaces.Checkout.Payments;
using Grand.Business.Core.Interfaces.Checkout.Shipping;
using Grand.Business.Core.Interfaces.Common.Addresses;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Common.Stores;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Business.Core.Interfaces.Storage;
using Grand.Domain.Common;
using Grand.Domain.Directory;
using Grand.Domain.Orders;
using Grand.Domain.Payments;
using Grand.Domain.Shipping;
using Grand.Domain.Tax;
using Grand.Infrastructure;
using Grand.Web.Common.Extensions;
using Grand.Web.Vendor.Extensions;
using Grand.Web.Vendor.Interfaces;
using Grand.Web.Vendor.Models.Orders;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Net;

namespace Grand.Web.Vendor.Services;

public class OrderViewModelService : IOrderViewModelService
{
    #region Ctor

    public OrderViewModelService(IOrderService orderService,
        IDateTimeService dateTimeService,
        IPriceFormatter priceFormatter,
        ITranslationService translationService,
        IWorkContext workContext,
        ICurrencyService currencyService,
        IPaymentService paymentService,
        ICountryService countryService,
        IProductService productService,
        IGiftVoucherService giftVoucherService,
        IDownloadService downloadService,
        IStoreService storeService,
        IVendorService vendorService,
        IAddressAttributeParser addressAttributeParser,
        IPictureService pictureService,
        IMerchandiseReturnService merchandiseReturnService,
        ICustomerService customerService,
        IWarehouseService warehouseService,
        CurrencySettings currencySettings,
        TaxSettings taxSettings,
        AddressSettings addressSettings,
        IOrderTagService orderTagService,
        IOrderStatusService orderStatusService)
    {
        _orderService = orderService;
        _dateTimeService = dateTimeService;
        _priceFormatter = priceFormatter;
        _translationService = translationService;
        _workContext = workContext;
        _currencyService = currencyService;
        _paymentService = paymentService;
        _countryService = countryService;
        _productService = productService;
        _giftVoucherService = giftVoucherService;
        _downloadService = downloadService;
        _storeService = storeService;
        _vendorService = vendorService;
        _addressAttributeParser = addressAttributeParser;
        _pictureService = pictureService;
        _merchandiseReturnService = merchandiseReturnService;
        _warehouseService = warehouseService;
        _currencySettings = currencySettings;
        _taxSettings = taxSettings;
        _addressSettings = addressSettings;
        _customerService = customerService;
        _orderTagService = orderTagService;
        _orderStatusService = orderStatusService;
    }

    #endregion

    public virtual async Task<OrderListModel> PrepareOrderListModel(
        int? orderStatusId = null,
        int? paymentStatusId = null,
        int? shippingStatusId = null,
        DateTime? startDate = null,
        string code = null)
    {
        //order statuses
        var statuses = await _orderStatusService.GetAll();
        var model = new OrderListModel {
            AvailableOrderStatuses = statuses
                .Select(x => new SelectListItem { Value = x.StatusId.ToString(), Text = x.Name }).ToList()
        };
        model.AvailableOrderStatuses.Insert(0,
            new SelectListItem { Text = _translationService.GetResource("Vendor.Common.All"), Value = " " });
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
            new SelectListItem { Text = _translationService.GetResource("Vendor.Common.All"), Value = " " });
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
            { Text = _translationService.GetResource("Vendor.Common.All"), Value = " " });
        foreach (var s in await _orderTagService.GetAllOrderTags())
            model.AvailableOrderTags.Add(new SelectListItem { Text = s.Name, Value = s.Id });

        //shipping statuses
        model.AvailableShippingStatuses =
            ShippingStatus.Pending.ToSelectList(_translationService, _workContext, false).ToList();
        model.AvailableShippingStatuses.Insert(0,
            new SelectListItem { Text = _translationService.GetResource("Vendor.Common.All"), Value = " " });
        if (shippingStatusId.HasValue)
        {
            //pre-select value?
            var item = model.AvailableShippingStatuses.FirstOrDefault(x =>
                x.Value == shippingStatusId.Value.ToString());
            if (item != null)
                item.Selected = true;
        }

        //warehouses
        model.AvailableWarehouses.Add(new SelectListItem
            { Text = _translationService.GetResource("Vendor.Common.All"), Value = " " });
        foreach (var w in await _warehouseService.GetAllWarehouses())
            model.AvailableWarehouses.Add(new SelectListItem { Text = w.Name, Value = w.Id });

        //payment methods
        model.AvailablePaymentMethods.Add(new SelectListItem
            { Text = _translationService.GetResource("Vendor.Common.All"), Value = " " });
        foreach (var pm in _paymentService.LoadAllPaymentMethods())
            model.AvailablePaymentMethods.Add(new SelectListItem { Text = pm.FriendlyName, Value = pm.SystemName });

        //billing countries
        foreach (var c in await _countryService.GetAllCountriesForBilling(showHidden: true))
            model.AvailableCountries.Add(new SelectListItem { Text = c.Name, Value = c.Id });

        model.AvailableCountries.Insert(0,
            new SelectListItem { Text = _translationService.GetResource("Vendor.Common.All"), Value = " " });

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
            model.ShippingStatusId > 0 ? (ShippingStatus?)model.ShippingStatusId : null;

        //load orders
        var orders = await _orderService.SearchOrders(
            vendorId: _workContext.CurrentVendor.Id,
            customerId: model.CustomerId,
            productId: model.ProductId,
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
            items.Add(new OrderModel {
                Id = x.Id,
                OrderNumber = x.OrderNumber,
                Code = x.Code,
                StoreName = store != null ? store.Shortcut : "Unknown",
                CurrencyCode = x.CustomerCurrencyCode,
                OrderStatus = status.FirstOrDefault(y => y.StatusId == x.OrderStatusId)?.Name,
                OrderStatusId = x.OrderStatusId,
                PaymentStatus = x.PaymentStatusId.GetTranslationEnum(_translationService, _workContext),
                CustomerEmail = x.BillingAddress?.Email,
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
        model.OrderStatus = status.FirstOrDefault(x => x.StatusId == order.OrderStatusId)?.Name;

        var store = await _storeService.GetStoreById(order.StoreId);
        model.StoreName = store != null ? store.Shortcut : "Unknown";
        model.UserFields = order.UserFields;

        var customer = await _customerService.GetCustomerById(order.CustomerId);
        if (customer != null)
            model.CustomerInfo = !string.IsNullOrEmpty(customer.Email)
                ? customer.Email
                : _translationService.GetResource("Vendor.Customers.Guest");

        model.VatNumber = order.VatNumber;
        model.CreatedOn = _dateTimeService.ConvertToUserTime(order.CreatedOnUtc, DateTimeKind.Utc);
        model.TaxDisplayType = _taxSettings.TaxDisplayType;

        #region Order totals

        var primaryStoreCurrency = await _currencyService.GetCurrencyByCode(order.PrimaryCurrencyCode) ??
                                   await _currencyService.GetCurrencyById(_currencySettings.PrimaryStoreCurrencyId);

        if (primaryStoreCurrency == null)
            throw new Exception("Cannot load primary store currency");

        var orderCurrency = await _currencyService.GetCurrencyByCode(order.CustomerCurrencyCode);
        if (orderCurrency == null)
            throw new Exception("Cannot load order currency");

        model.CurrencyRate = order.CurrencyRate;
        model.CurrencyCode = order.CustomerCurrencyCode;

        #endregion

        #region Payment info

        //payment method info
        var pm = _paymentService.LoadPaymentMethodBySystemName(order.PaymentMethodSystemName);
        model.PaymentMethod = pm != null ? pm.FriendlyName : order.PaymentMethodSystemName;
        model.PaymentStatus = order.PaymentStatusId.GetTranslationEnum(_translationService, _workContext);
        model.PaymentStatusEnum = order.PaymentStatusId;

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
        var products = order.OrderItems
            .Where(orderItem => orderItem.VendorId == _workContext.CurrentVendor.Id)
            .ToList();

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
                    _translationService.GetResource("Vendor.Orders.Products.RecurringPeriod"),
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

        //load orders
        var orders = await _orderService.SearchOrders(
            vendorId: _workContext.CurrentVendor.Id,
            productId: model.ProductId,
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

    #region Fields

    private readonly IOrderService _orderService;
    private readonly IDateTimeService _dateTimeService;
    private readonly IPriceFormatter _priceFormatter;
    private readonly ITranslationService _translationService;
    private readonly IWorkContext _workContext;
    private readonly ICurrencyService _currencyService;
    private readonly IPaymentService _paymentService;
    private readonly ICountryService _countryService;
    private readonly IProductService _productService;
    private readonly IGiftVoucherService _giftVoucherService;
    private readonly IDownloadService _downloadService;
    private readonly IStoreService _storeService;
    private readonly IVendorService _vendorService;
    private readonly IAddressAttributeParser _addressAttributeParser;
    private readonly IPictureService _pictureService;
    private readonly IMerchandiseReturnService _merchandiseReturnService;
    private readonly ICustomerService _customerService;
    private readonly IWarehouseService _warehouseService;
    private readonly CurrencySettings _currencySettings;
    private readonly TaxSettings _taxSettings;
    private readonly AddressSettings _addressSettings;
    private readonly IOrderTagService _orderTagService;
    private readonly IOrderStatusService _orderStatusService;

    #endregion
}