using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Catalog.Prices;
using Grand.Business.Core.Interfaces.Catalog.Tax;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Domain.Orders;
using Grand.Domain.Shipping;
using Grand.Web.Features.Models.Products;
using Grand.Web.Models.Catalog;
using Grand.Mediator;

namespace Grand.Web.Features.Handlers.Products;

public class GetProductStructuredDataHandler : IRequestHandler<GetProductStructuredData, ProductStructuredDataModel>
{
    private readonly ICountryService _countryService;
    private readonly ICurrencyService _currencyService;
    private readonly OrderSettings _orderSettings;
    private readonly IPricingService _pricingService;
    private readonly ShippingSettings _shippingSettings;
    private readonly ITaxService _taxService;

    public GetProductStructuredDataHandler(
        ICountryService countryService,
        ICurrencyService currencyService,
        IPricingService pricingService,
        ITaxService taxService,
        OrderSettings orderSettings,
        ShippingSettings shippingSettings)
    {
        _countryService = countryService;
        _currencyService = currencyService;
        _pricingService = pricingService;
        _taxService = taxService;
        _orderSettings = orderSettings;
        _shippingSettings = shippingSettings;
    }

    public async Task<ProductStructuredDataModel> Handle(GetProductStructuredData request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request.Product);

        var product = request.Product;
        var model = new ProductStructuredDataModel();

        //the Approved* totals count approved reviews only (UpdateProductReviewTotalsCommandHandler),
        //and they are exactly what the reviews tab lists - so the rating describes reviews on the page
        if (product.AllowCustomerReviews && product.ApprovedTotalReviews > 0)
        {
            model.RatingValue = Math.Round((double)product.ApprovedRatingSum / product.ApprovedTotalReviews, 2);
            model.ReviewCount = product.ApprovedTotalReviews;
        }

        if (product.IsShipEnabled)
        {
            model.ShippingRate = await ShippingRate(request);
            model.HandlingTime = DayRange(_shippingSettings.HandlingTimeMinDays, _shippingSettings.HandlingTimeMaxDays);
            model.TransitTime = DayRange(_shippingSettings.TransitTimeMinDays, _shippingSettings.TransitTimeMaxDays);
        }

        //the Organization declares the store's return policy; a product excluded from returns must say so
        model.ReturnsNotPermitted = _orderSettings.MerchandiseReturnsEnabled && product.NotReturnable;

        if (model.HasShippingDetails || model.ReturnsNotPermitted)
            model.CountryCodes = (await _countryService.GetAllCountriesForShipping(storeId: request.Store?.Id ?? ""))
                .Select(x => x.TwoLetterIsoCode)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct()
                .ToList();

        return model;
    }

    /// <summary>
    ///     What shipping this product on its own costs, in the request currency, or null when the store's
    ///     data does not say. Mirrors OrderCalculationService.IsFreeShipping / AdjustShippingRate for a cart
    ///     holding just this product; the shipping providers are never asked - their rates depend on the
    ///     address and the cart, so the fallback is the rate the store declares in ShippingSettings.
    ///     Visitor-specific free shipping (customer, customer group) and shipping discounts are left out:
    ///     they are not a property of the product.
    /// </summary>
    private async Task<double?> ShippingRate(GetProductStructuredData request)
    {
        var product = request.Product;
        if (product.IsFreeShipping)
            return 0;

        if (request.Customer == null || request.Currency == null)
            return null;

        //free over X compares the cart subtotal, in the working currency, with the threshold as entered
        if (_shippingSettings.FreeShippingOverXEnabled && await UnitPrice(request) > _shippingSettings.FreeShippingOverXValue)
            return 0;

        if (_shippingSettings.DefaultShippingRate is not >= 0)
            return null;

        //entered like a shipping method's rate: primary store currency, converted the way the rate providers
        //convert theirs, plus the product's own surcharge, taxed as shipping
        var rate = await _currencyService.ConvertFromPrimaryStoreCurrency(
            _shippingSettings.DefaultShippingRate.Value + product.AdditionalShippingCharge, request.Currency);
        rate = (await _taxService.GetShippingPrice(rate, request.Customer, request.Store)).shippingPrice;
        return RoundingHelper.RoundPrice(rate, request.Currency);
    }

    private async Task<double> UnitPrice(GetProductStructuredData request)
    {
        var finalPrice = (await _pricingService.GetFinalPrice(request.Product, request.Customer, request.Store,
            request.Currency, includeDiscounts: true)).finalPrice;
        return (await _taxService.GetProductPrice(request.Product, finalPrice,
            _shippingSettings.FreeShippingOverXIncludingTax, request.Customer, request.Store)).productprice;
    }

    //the settings screen does not validate these optional fields, so a pair that cannot be
    //true (a negative day count, min above max) is dropped rather than published
    private static ProductStructuredDataModel.DayRangeModel DayRange(int? min, int? max)
    {
        if (min < 0 || max < 0 || min > max) return null;
        if (min == null && max == null) return null;
        return new ProductStructuredDataModel.DayRangeModel { Min = min, Max = max };
    }
}
