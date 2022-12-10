﻿using Grand.Business.Core.Commands.Messages.Tokens;
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
using Grand.Domain.Vendors;
using MediatR;
using System.Globalization;
using System.Net;

namespace Grand.Business.System.Commands.Handlers.Messages
{
    public class GetOrderTokensCommandHandler : IRequestHandler<GetOrderTokensCommand, LiquidOrder>
    {
        private readonly ILanguageService _languageService;
        private readonly ICurrencyService _currencyService;
        private readonly IProductService _productService;
        private readonly IVendorService _vendorService;
        private readonly IPriceFormatter _priceFormatter;
        private readonly ICountryService _countryService;
        private readonly IAddressAttributeParser _addressAttributeParser;
        private readonly IPaymentService _paymentService;
        private readonly ITranslationService _translationService;
        private readonly IGiftVoucherService _giftVoucherService;

        private readonly TaxSettings _taxSettings;

        public GetOrderTokensCommandHandler(
            ILanguageService languageService,
            ICurrencyService currencyService,
            IProductService productService,
            IVendorService vendorService,
            IPriceFormatter priceFormatter,
            ICountryService countryService,
            IAddressAttributeParser addressAttributeParser,
            IPaymentService paymentService,
            ITranslationService translationService,
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
            _translationService = translationService;
            _giftVoucherService = giftVoucherService;
            _taxSettings = taxSettings;
        }

        public async Task<LiquidOrder> Handle(GetOrderTokensCommand request, CancellationToken cancellationToken)
        {
            var language = await _languageService.GetLanguageById(request.Order.CustomerLanguageId);
            var currency = await _currencyService.GetCurrencyByCode(request.Order.CustomerCurrencyCode);

            var liquidOrder = new LiquidOrder(request.Order, request.Customer, language, currency, request.Store, request.Host, request.OrderNote, request.Vendor);
            foreach (var item in request.Order.OrderItems.Where(x => x.VendorId == request.Vendor?.Id || request.Vendor == null))
            {
                var product = await _productService.GetProductById(item.ProductId);
                Vendor vendorItem = string.IsNullOrEmpty(item.VendorId) ? null : await _vendorService.GetVendorById(item.VendorId);
                var liquidItem = new LiquidOrderItem(item, product, language, request.Store, request.Host, vendorItem);

                #region Download

                liquidItem.IsDownloadAllowed = request.Order.IsDownloadAllowed(item, product);
                liquidItem.IsLicenseDownloadAllowed = request.Order.IsLicenseDownloadAllowed(item, product);

                #endregion

                #region Unit price

                var unitPriceStr =
                    //including tax
                    request.Order.CustomerTaxDisplayTypeId == TaxDisplayType.IncludingTax ? _priceFormatter.FormatPrice(item.UnitPriceInclTax, currency, language, true) :
                    //excluding tax
                    _priceFormatter.FormatPrice(item.UnitPriceExclTax, currency, language, false);
                liquidItem.UnitPrice = unitPriceStr;

                #endregion

                #region total price

                var priceStr =
                    //including tax
                    request.Order.CustomerTaxDisplayTypeId == TaxDisplayType.IncludingTax ? _priceFormatter.FormatPrice(item.PriceInclTax, currency, language, true) :
                    //excluding tax
                    _priceFormatter.FormatPrice(item.PriceExclTax, currency, language, false);
                liquidItem.TotalPrice = priceStr;

                #endregion

                var sku = "";
                if (product != null)
                    sku = product.FormatSku(item.Attributes);

                liquidItem.ProductSku = WebUtility.HtmlEncode(sku);
                liquidOrder.OrderItems.Add(liquidItem);
            }
            var billingCountry = await _countryService.GetCountryById(request.Order.BillingAddress?.CountryId);
            liquidOrder.BillingCustomAttributes = await _addressAttributeParser.FormatAttributes(language, request.Order.BillingAddress?.Attributes);
            liquidOrder.BillingCountry = request.Order.BillingAddress != null && !string.IsNullOrEmpty(request.Order.BillingAddress.CountryId) ? billingCountry?.GetTranslation(x => x.Name, request.Order.CustomerLanguageId) : "";
            liquidOrder.BillingStateProvince = !string.IsNullOrEmpty(request.Order.BillingAddress?.StateProvinceId) ? billingCountry?.StateProvinces.FirstOrDefault(x => x.Id == request.Order.BillingAddress.StateProvinceId)?.GetTranslation(x => x.Name, request.Order.CustomerLanguageId) : "";

            var shippingCountry = await _countryService.GetCountryById(request.Order.ShippingAddress?.CountryId);
            liquidOrder.ShippingCountry = request.Order.ShippingAddress != null && !string.IsNullOrEmpty(request.Order.ShippingAddress.CountryId) ? shippingCountry?.GetTranslation(x => x.Name, request.Order.CustomerLanguageId) : "";
            liquidOrder.ShippingStateProvince = request.Order.ShippingAddress != null && !string.IsNullOrEmpty(request.Order.ShippingAddress.StateProvinceId) ? shippingCountry?.StateProvinces.FirstOrDefault(x => x.Id == request.Order.ShippingAddress.StateProvinceId)?.GetTranslation(x => x.Name, request.Order.CustomerLanguageId) : "";
            liquidOrder.ShippingCustomAttributes = await _addressAttributeParser.FormatAttributes(language, request.Order.ShippingAddress?.Attributes);

            var paymentMethod = _paymentService.LoadPaymentMethodBySystemName(request.Order.PaymentMethodSystemName);
            liquidOrder.PaymentMethod = paymentMethod != null ? paymentMethod.FriendlyName : request.Order.PaymentMethodSystemName;

            var dict = new Dictionary<string, string>();
            foreach (var item in request.Order.OrderTaxes)
            {
                var taxRate = string.Format(_translationService.GetResource("Messages.Order.TaxRateLine", language.Id), _priceFormatter.FormatTaxRate(item.Percent));
                var taxValue = _priceFormatter.FormatPrice(item.Amount, currency, language, false);

                if (string.IsNullOrEmpty(taxRate))
                    taxRate = item.Percent.ToString(CultureInfo.InvariantCulture);

                if (!dict.ContainsKey(taxRate))
                    dict.Add(taxRate, taxValue);
            }

            liquidOrder.TaxRates = dict;

            var cards = new Dictionary<string, string>();
            var gcuhC = await _giftVoucherService.GetAllGiftVoucherUsageHistory(request.Order.Id);
            foreach (var gcuh in gcuhC)
            {
                var giftVoucher = await _giftVoucherService.GetGiftVoucherById(gcuh.GiftVoucherId);
                var giftVoucherText = string.Format(_translationService.GetResource("Messages.Order.GiftVoucherInfo", language.Id), WebUtility.HtmlEncode(giftVoucher.Code));
                var giftVoucherAmount = _priceFormatter.FormatPrice(-gcuh.UsedValue, currency, language, true, false);
                cards.Add(giftVoucherText, giftVoucherAmount);
            }
            liquidOrder.GiftVouchers = cards;
            if (request.Order.RedeemedLoyaltyPoints > 0)
            {
                liquidOrder.RPTitle = string.Format(_translationService.GetResource("Messages.Order.LoyaltyPoints", language.Id), -request.Order.RedeemedLoyaltyPoints);
                liquidOrder.RPAmount = _priceFormatter.FormatPrice(-request.Order.RedeemedLoyaltyPointsAmount, currency, language, true, false);
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
                if (request.Order.CustomerTaxDisplayTypeId == TaxDisplayType.IncludingTax && !_taxSettings.ForceTaxExclusionFromOrderSubtotal)
                {
                    //including tax

                    //subtotal
                    _cusSubTotal = _priceFormatter.FormatPrice(request.Order.OrderSubtotalInclTax, currency, language, true);
                    //discount (applied to order subtotal)
                    if (request.Order.OrderSubTotalDiscountInclTax > 0)
                    {
                        _cusSubTotalDiscount = _priceFormatter.FormatPrice(-request.Order.OrderSubTotalDiscountInclTax, currency, language, true);
                        _displaySubTotalDiscount = true;
                    }
                }
                else
                {
                    //subtotal
                    _cusSubTotal = _priceFormatter.FormatPrice(request.Order.OrderSubtotalExclTax, currency, language, false);
                    //discount (applied to order subtotal)
                    if (request.Order.OrderSubTotalDiscountExclTax > 0)
                    {
                        _cusSubTotalDiscount = _priceFormatter.FormatPrice(-request.Order.OrderSubTotalDiscountExclTax, currency, language, false);
                        _displaySubTotalDiscount = true;
                    }
                }

                //shipping, payment method fee
                _cusTaxTotal = string.Empty;
                _cusDiscount = string.Empty;

                if (request.Order.CustomerTaxDisplayTypeId == TaxDisplayType.IncludingTax)
                {
                    //including tax

                    //shipping
                    _cusShipTotal = _priceFormatter.FormatShippingPrice(request.Order.OrderShippingInclTax, currency, language, true);
                    //payment method additional fee
                    _cusPaymentMethodAdditionalFee = _priceFormatter.FormatPaymentMethodAdditionalFee(request.Order.PaymentMethodAdditionalFeeInclTax, currency, language, true);
                }
                else
                {
                    //excluding tax

                    //shipping
                    _cusShipTotal = _priceFormatter.FormatShippingPrice(request.Order.OrderShippingExclTax, currency, language, false);
                    //payment method additional fee
                    _cusPaymentMethodAdditionalFee = _priceFormatter.FormatPaymentMethodAdditionalFee(request.Order.PaymentMethodAdditionalFeeExclTax, currency, language, false);
                }
                if (_taxSettings.HideTaxInOrderSummary && request.Order.CustomerTaxDisplayTypeId == TaxDisplayType.IncludingTax)
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

                        var taxStr = _priceFormatter.FormatPrice(request.Order.OrderTax, currency, language, request.Order.CustomerTaxDisplayTypeId == TaxDisplayType.IncludingTax, false);
                        _cusTaxTotal = taxStr;
                    }
                }

                //discount
                _displayDiscount = false;
                if (request.Order.OrderDiscount > 0)
                {
                    _cusDiscount = _priceFormatter.FormatPrice(-request.Order.OrderDiscount, currency, language, request.Order.CustomerTaxDisplayTypeId == TaxDisplayType.IncludingTax, false);
                    _displayDiscount = true;
                }

                //total
                _cusTotal = _priceFormatter.FormatPrice(request.Order.OrderTotal, currency, language, request.Order.CustomerTaxDisplayTypeId == TaxDisplayType.IncludingTax, false);


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
}
