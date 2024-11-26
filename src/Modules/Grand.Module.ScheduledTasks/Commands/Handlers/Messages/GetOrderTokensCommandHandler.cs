using Grand.Business.Core.Commands.Messages.Tokens;
using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Catalog.Prices;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Checkout.GiftVouchers;
using Grand.Business.Core.Interfaces.Checkout.Payments;
using Grand.Business.Core.Interfaces.Common.Addresses;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Interfaces.Common.Localization;
using Grand.Business.Core.Interfaces.Customers;
using Grand.Business.Core.Utilities.Messages.DotLiquidDrops;
using Grand.Domain.Catalog;
using Grand.Domain.Orders;
using Grand.Domain.Tax;
using MediatR;
using System.Globalization;
using System.Net;

namespace Grand.Business.System.Commands.Handlers.Messages;

public class GetOrderTokensCommandHandler : IRequestHandler<GetOrderTokensCommand, LiquidOrder>
{
    private readonly IAddressAttributeParser _addressAttributeParser;
    private readonly ICountryService _countryService;
    private readonly ICurrencyService _currencyService;
    private readonly IGiftVoucherService _giftVoucherService;
    private readonly ILanguageService _languageService;
    private readonly IPaymentService _paymentService;
    private readonly IPriceFormatter _priceFormatter;
    private readonly IProductService _productService;

    private readonly TaxSettings _taxSettings;
    private readonly IVendorService _vendorService;

    public GetOrderTokensCommandHandler(
        ILanguageService languageService,
        ICurrencyService currencyService,
        IProductService productService,
        IVendorService vendorService,
        IPriceFormatter priceFormatter,
        ICountryService countryService,
        IAddressAttributeParser addressAttributeParser,
        IPaymentService paymentService,
        IGiftVoucherService giftVoucherService,
        TaxSettings taxSettings)
    {
        _languageService = languageService;
        _currencyService = currencyService;
        _productService = productService;
        _vendorService = vendorService;
        _priceFormatter = priceFormatter;
        _countryService = countryService;
        _addressAttributeParser = addressAttributeParser;
        _paymentService = paymentService;
        _giftVoucherService = giftVoucherService;
        _taxSettings = taxSettings;
    }

    public async Task<LiquidOrder> Handle(GetOrderTokensCommand request, CancellationToken cancellationToken)
    {
        var language = await _languageService.GetLanguageById(request.Order.CustomerLanguageId);
        var currency = await _currencyService.GetCurrencyByCode(request.Order.CustomerCurrencyCode);

        var liquidOrder = new LiquidOrder(request.Order, request.Customer, language, currency, request.Store,
            request.Host, request.OrderNote, request.Vendor);
        foreach (var item in request.Order.OrderItems.Where(x =>
                     x.VendorId == request.Vendor?.Id || request.Vendor == null))
        {
            var product = await _productService.GetProductById(item.ProductId);
            var vendorItem = string.IsNullOrEmpty(item.VendorId)
                ? null
                : await _vendorService.GetVendorById(item.VendorId);
            var liquidItem = new LiquidOrderItem(item, product, language, request.Store, request.Host, vendorItem);

            #region Download

            liquidItem.IsDownloadAllowed = request.Order.IsDownloadAllowed(item, product);
            liquidItem.IsLicenseDownloadAllowed = request.Order.IsLicenseDownloadAllowed(item, product);

            #endregion

            #region Unit price

            liquidItem.UnitPrice = _priceFormatter.FormatPrice(item.UnitPriceInclTax, currency);
            liquidItem.UnitPriceWithTax = request.Order.CustomerTaxDisplayTypeId == TaxDisplayType.IncludingTax;

            #endregion

            #region total price

            liquidItem.TotalPrice = _priceFormatter.FormatPrice(item.PriceInclTax, currency);
            liquidItem.TotalPriceWithTax = request.Order.CustomerTaxDisplayTypeId == TaxDisplayType.IncludingTax;

            #endregion

            var sku = "";
            if (product != null)
                sku = product.FormatSku(item.Attributes);

            liquidItem.ProductSku = WebUtility.HtmlEncode(sku);
            liquidOrder.OrderItems.Add(liquidItem);
        }

        var billingCountry = await _countryService.GetCountryById(request.Order.BillingAddress?.CountryId);
        liquidOrder.BillingCustomAttributes =
            await _addressAttributeParser.FormatAttributes(language, request.Order.BillingAddress?.Attributes);
        liquidOrder.BillingCountry =
            request.Order.BillingAddress != null && !string.IsNullOrEmpty(request.Order.BillingAddress.CountryId)
                ? billingCountry?.GetTranslation(x => x.Name, request.Order.CustomerLanguageId)
                : "";
        liquidOrder.BillingStateProvince = !string.IsNullOrEmpty(request.Order.BillingAddress?.StateProvinceId)
            ? billingCountry?.StateProvinces.FirstOrDefault(x => x.Id == request.Order.BillingAddress.StateProvinceId)
                ?.GetTranslation(x => x.Name, request.Order.CustomerLanguageId)
            : "";

        var shippingCountry = await _countryService.GetCountryById(request.Order.ShippingAddress?.CountryId);
        liquidOrder.ShippingCountry =
            request.Order.ShippingAddress != null && !string.IsNullOrEmpty(request.Order.ShippingAddress.CountryId)
                ? shippingCountry?.GetTranslation(x => x.Name, request.Order.CustomerLanguageId)
                : "";
        liquidOrder.ShippingStateProvince =
            request.Order.ShippingAddress != null &&
            !string.IsNullOrEmpty(request.Order.ShippingAddress.StateProvinceId)
                ? shippingCountry?.StateProvinces
                    .FirstOrDefault(x => x.Id == request.Order.ShippingAddress.StateProvinceId)
                    ?.GetTranslation(x => x.Name, request.Order.CustomerLanguageId)
                : "";
        liquidOrder.ShippingCustomAttributes =
            await _addressAttributeParser.FormatAttributes(language, request.Order.ShippingAddress?.Attributes);

        var paymentMethod = _paymentService.LoadPaymentMethodBySystemName(request.Order.PaymentMethodSystemName);
        liquidOrder.PaymentMethod =
            paymentMethod != null ? paymentMethod.FriendlyName : request.Order.PaymentMethodSystemName;

        var dict = new Dictionary<string, string>();
        foreach (var item in request.Order.OrderTaxes)
        {
            var taxRate = _priceFormatter.FormatTaxRate(item.Percent);
            var taxValue = _priceFormatter.FormatPrice(item.Amount);

            if (string.IsNullOrEmpty(taxRate))
                taxRate = item.Percent.ToString(CultureInfo.InvariantCulture);

            dict.TryAdd(taxRate, taxValue);
        }

        liquidOrder.TaxRates = dict;

        var cards = new Dictionary<string, string>();
        var gcuhC = await _giftVoucherService.GetAllGiftVoucherUsageHistory(request.Order.Id);
        foreach (var gcuh in gcuhC)
        {
            var giftVoucher = await _giftVoucherService.GetGiftVoucherById(gcuh.GiftVoucherId);
            var giftVoucherText = WebUtility.HtmlEncode(giftVoucher.Code);
            var giftVoucherAmount = _priceFormatter.FormatPrice(-gcuh.UsedValue, currency);
            cards.Add(giftVoucherText, giftVoucherAmount);
        }

        liquidOrder.GiftVouchers = cards;
        if (request.Order.RedeemedLoyaltyPoints > 0)
        {
            liquidOrder.RPPoints = request.Order.RedeemedLoyaltyPoints.ToString();
            liquidOrder.RPAmount = _priceFormatter.FormatPrice(-request.Order.RedeemedLoyaltyPointsAmount, currency);
        }

        void CalculateSubTotals()
        {
            string _cusSubTotal;
            bool _displaySubTotalDiscount;
            string _cusSubTotalDiscount;
            string _cusShipTotal;
            string _cusPaymentMethodAdditionalFee;
            bool _displayTax;
            string _cusTaxTotal;
            bool _displayTaxRates;
            bool _displayDiscount;
            string _cusDiscount;
            string _cusTotal;

            _displaySubTotalDiscount = false;
            _cusSubTotalDiscount = string.Empty;
            if (request.Order.CustomerTaxDisplayTypeId == TaxDisplayType.IncludingTax &&
                !_taxSettings.ForceTaxExclusionFromOrderSubtotal)
            {
                //subtotal
                _cusSubTotal = _priceFormatter.FormatPrice(request.Order.OrderSubtotalInclTax, currency);
                //discount (applied to order subtotal)
                if (request.Order.OrderSubTotalDiscountInclTax > 0)
                {
                    _cusSubTotalDiscount =
                        _priceFormatter.FormatPrice(-request.Order.OrderSubTotalDiscountInclTax, currency);
                    _displaySubTotalDiscount = true;
                }
            }
            else
            {
                //subtotal
                _cusSubTotal = _priceFormatter.FormatPrice(request.Order.OrderSubtotalExclTax, currency);
                //discount (applied to order subtotal)
                if (request.Order.OrderSubTotalDiscountExclTax > 0)
                {
                    _cusSubTotalDiscount =
                        _priceFormatter.FormatPrice(-request.Order.OrderSubTotalDiscountExclTax, currency);
                    _displaySubTotalDiscount = true;
                }
            }

            //shipping, payment method fee
            _cusTaxTotal = string.Empty;
            _cusDiscount = string.Empty;

            if (request.Order.CustomerTaxDisplayTypeId == TaxDisplayType.IncludingTax)
            {
                //shipping
                _cusShipTotal = _priceFormatter.FormatPrice(request.Order.OrderShippingInclTax, currency);
                //payment method additional fee
                _cusPaymentMethodAdditionalFee =
                    _priceFormatter.FormatPrice(request.Order.PaymentMethodAdditionalFeeInclTax, currency);
            }
            else
            {
                //shipping
                _cusShipTotal = _priceFormatter.FormatPrice(request.Order.OrderShippingExclTax, currency);
                //payment method additional fee
                _cusPaymentMethodAdditionalFee =
                    _priceFormatter.FormatPrice(request.Order.PaymentMethodAdditionalFeeExclTax, currency);
            }

            if (_taxSettings.HideTaxInOrderSummary &&
                request.Order.CustomerTaxDisplayTypeId == TaxDisplayType.IncludingTax)
            {
                _displayTax = false;
                _displayTaxRates = false;
            }
            else
            {
                if (request.Order.OrderTax == 0 && _taxSettings.HideZeroTax)
                {
                    _displayTax = false;
                    _displayTaxRates = false;
                }
                else
                {
                    _displayTaxRates = _taxSettings.DisplayTaxRates && request.Order.OrderTaxes.Any();
                    _displayTax = !_displayTaxRates;

                    var taxStr = _priceFormatter.FormatPrice(request.Order.OrderTax, currency);
                    _cusTaxTotal = taxStr;
                }
            }

            //discount
            _displayDiscount = false;
            if (request.Order.OrderDiscount > 0)
            {
                _cusDiscount = _priceFormatter.FormatPrice(-request.Order.OrderDiscount, currency);
                _displayDiscount = true;
            }

            //total
            _cusTotal = _priceFormatter.FormatPrice(request.Order.OrderTotal, currency);

            liquidOrder.SubTotal = _cusSubTotal;
            liquidOrder.DisplaySubTotalDiscount = _displaySubTotalDiscount;
            liquidOrder.SubTotalDiscount = _cusSubTotalDiscount;
            liquidOrder.Shipping = _cusShipTotal;
            liquidOrder.Discount = _cusDiscount;
            liquidOrder.PaymentMethodAdditionalFee = _cusPaymentMethodAdditionalFee;
            liquidOrder.Tax = _cusTaxTotal;
            liquidOrder.Total = _cusTotal;
            liquidOrder.DisplayTax = _displayTax;
            liquidOrder.DisplayDiscount = _displayDiscount;
            liquidOrder.DisplayTaxRates = _displayTaxRates;
        }

        CalculateSubTotals();

        return liquidOrder;
    }
}