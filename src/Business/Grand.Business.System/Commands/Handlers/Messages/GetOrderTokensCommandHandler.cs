using Grand.Business.Catalog.Extensions;
using Grand.Business.Catalog.Interfaces.Prices;
using Grand.Business.Catalog.Interfaces.Products;
using Grand.Business.Checkout.Extensions;
using Grand.Business.Checkout.Interfaces.GiftVouchers;
using Grand.Business.Checkout.Interfaces.Payments;
using Grand.Business.Common.Extensions;
using Grand.Business.Common.Interfaces.Addresses;
using Grand.Business.Common.Interfaces.Directory;
using Grand.Business.Common.Interfaces.Localization;
using Grand.Business.Customers.Interfaces;
using Grand.Business.Messages.Commands.Models;
using Grand.Business.Messages.DotLiquidDrops;
using Grand.Domain.Catalog;
using Grand.Domain.Shipping;
using Grand.Domain.Tax;
using Grand.Domain.Vendors;
using MediatR;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Grand.Business.System.Commands.Handlers.Messages
{
    public class GetOrderTokensCommandHandler : IRequestHandler<GetOrderTokensCommand, LiquidOrder>
    {
        private readonly ILanguageService _languageService;
        private readonly ICurrencyService _currencyService;
        private readonly IProductService _productService;
        private readonly IVendorService _vendorService;
        private readonly IPriceFormatter _priceFormatter;
        private readonly IProductAttributeParser _productAttributeParser;
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
            IProductAttributeParser productAttributeParser,
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
            _productAttributeParser = productAttributeParser;
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

            var liquidOrder = new LiquidOrder(request.Order, request.Customer, language, currency, request.Store, request.OrderNote, request.Vendor);
            foreach (var item in request.Order.OrderItems.Where(x => x.VendorId == request.Vendor?.Id || request.Vendor == null))
            {
                var product = await _productService.GetProductById(item.ProductId);
                Vendor vendorItem = string.IsNullOrEmpty(item.VendorId) ? null : await _vendorService.GetVendorById(item.VendorId);
                var liqitem = new LiquidOrderItem(item, product, language, request.Store, vendorItem);

                #region Download

                liqitem.IsDownloadAllowed = request.Order.IsDownloadAllowed(item, product);
                liqitem.IsLicenseDownloadAllowed = request.Order.IsLicenseDownloadAllowed(item, product);

                #endregion

                #region Unit price
                string unitPriceStr;
                if (request.Order.CustomerTaxDisplayTypeId == TaxDisplayType.IncludingTax)
                {
                    //including tax
                    unitPriceStr = _priceFormatter.FormatPrice(item.UnitPriceInclTax, currency, language, true);
                }
                else
                {
                    //excluding tax
                    unitPriceStr = _priceFormatter.FormatPrice(item.UnitPriceExclTax, currency, language, false);
                }
                liqitem.UnitPrice = unitPriceStr;

                #endregion

                #region total price
                string priceStr;
                if (request.Order.CustomerTaxDisplayTypeId == TaxDisplayType.IncludingTax)
                {
                    //including tax
                    priceStr = _priceFormatter.FormatPrice(item.PriceInclTax, currency, language, true);
                }
                else
                {
                    //excluding tax
                    priceStr = _priceFormatter.FormatPrice(item.PriceExclTax, currency, language, false);
                }
                liqitem.TotalPrice = priceStr;

                #endregion

                string sku = "";
                if (product != null)
                    sku = product.FormatSku(item.Attributes, _productAttributeParser);

                liqitem.ProductSku = WebUtility.HtmlEncode(sku);
                liquidOrder.OrderItems.Add(liqitem);
            }
            var billingCountry = await _countryService.GetCountryById(request.Order.BillingAddress?.CountryId);
            liquidOrder.BillingCustomAttributes = await _addressAttributeParser.FormatAttributes(language, request.Order.BillingAddress?.Attributes);
            liquidOrder.BillingCountry = request.Order.BillingAddress != null && !string.IsNullOrEmpty(request.Order.BillingAddress.CountryId) ? billingCountry?.GetTranslation(x => x.Name, request.Order.CustomerLanguageId) : "";
            liquidOrder.BillingStateProvince = !string.IsNullOrEmpty(request.Order.BillingAddress.StateProvinceId) ? billingCountry?.StateProvinces.FirstOrDefault(x => x.Id == request.Order.BillingAddress.StateProvinceId)?.GetTranslation(x => x.Name, request.Order.CustomerLanguageId) : "";

            var shippingCountry = await _countryService.GetCountryById(request.Order.ShippingAddress?.CountryId);
            liquidOrder.ShippingCountry = request.Order.ShippingAddress != null && !string.IsNullOrEmpty(request.Order.ShippingAddress.CountryId) ? shippingCountry?.GetTranslation(x => x.Name, request.Order.CustomerLanguageId) : "";
            liquidOrder.ShippingStateProvince = request.Order.ShippingAddress != null && !string.IsNullOrEmpty(request.Order.ShippingAddress.StateProvinceId) ? shippingCountry?.StateProvinces.FirstOrDefault(x => x.Id == request.Order.ShippingAddress.StateProvinceId)?.GetTranslation(x => x.Name, request.Order.CustomerLanguageId) : "";
            liquidOrder.ShippingCustomAttributes = await _addressAttributeParser.FormatAttributes(language, request.Order.ShippingAddress != null ? request.Order.ShippingAddress.Attributes : null);

            var paymentMethod = _paymentService.LoadPaymentMethodBySystemName(request.Order.PaymentMethodSystemName);
            liquidOrder.PaymentMethod = paymentMethod != null ? paymentMethod.FriendlyName : request.Order.PaymentMethodSystemName;
            liquidOrder.AmountRefunded = _priceFormatter.FormatPrice(request.RefundedAmount, currency, language, false);

            var dict = new Dictionary<string, string>();
            foreach (var item in request.Order.OrderTaxes)
            {
                var taxRate = string.Format(_translationService.GetResource("Messages.Order.TaxRateLine", language.Id), _priceFormatter.FormatTaxRate(item.Percent));
                var taxValue = _priceFormatter.FormatPrice(item.Amount, currency, language, false);

                if (string.IsNullOrEmpty(taxRate))
                    taxRate = item.Percent.ToString();

                if(!dict.ContainsKey(taxRate))
                    dict.Add(taxRate, taxValue);
            }

            liquidOrder.TaxRates = dict;

            Dictionary<string, string> cards = new Dictionary<string, string>();
            var gcuhC = await _giftVoucherService.GetAllGiftVoucherUsageHistory(request.Order.Id);
            foreach (var gcuh in gcuhC)
            {
                var giftVoucher = await _giftVoucherService.GetGiftVoucherById(gcuh.GiftVoucherId);
                string giftVoucherText = string.Format(_translationService.GetResource("Messages.Order.GiftVoucherInfo", language.Id), WebUtility.HtmlEncode(giftVoucher.Code));
                string giftVoucherAmount = _priceFormatter.FormatPrice(-gcuh.UsedValue, currency, language, true, false);
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
                    //exсluding tax

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

                //shipping
                bool displayShipping = request.Order.ShippingStatusId != ShippingStatus.ShippingNotRequired;

                //payment method fee
                bool displayPaymentMethodFee = request.Order.PaymentMethodAdditionalFeeExclTax > 0;

                //tax
                _displayTax = true;
                _displayTaxRates = true;
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

                        string taxStr = _priceFormatter.FormatPrice(request.Order.OrderTax, currency, language, request.Order.CustomerTaxDisplayTypeId == TaxDisplayType.IncludingTax, false);
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
