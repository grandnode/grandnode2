using Grand.Business.Core.Extensions;
using Grand.Business.Core.Interfaces.Catalog.Discounts;
using Grand.Business.Core.Interfaces.Catalog.Prices;
using Grand.Business.Core.Interfaces.Catalog.Products;
using Grand.Business.Core.Interfaces.Common.Directory;
using Grand.Business.Core.Queries.Catalog;
using Grand.Business.Core.Utilities.Catalog;
using Grand.Domain.Catalog;
using Grand.Domain.Common;
using Grand.Domain.Customers;
using Grand.Domain.Directory;
using Grand.Domain.Orders;
using Grand.Domain.Stores;
using Grand.Infrastructure;
using MediatR;
using System.Globalization;

namespace Grand.Business.Catalog.Services.Prices;

/// <summary>
///     Pricing service
/// </summary>
public class PricingService : IPricingService
{
    #region Ctor

    public PricingService(
        IContextAccessor contextAccessor,
        IProductService productService,
        IMediator mediator,
        ICurrencyService currencyService,
        IDiscountHandlerService discountApplicationService,
        ShoppingCartSettings shoppingCartSettings,
        CatalogSettings catalogSettings)
    {
        _contextAccessor = contextAccessor;
        _productService = productService;
        _mediator = mediator;
        _currencyService = currencyService;
        _shoppingCartSettings = shoppingCartSettings;
        _catalogSettings = catalogSettings;
        _discountApplicationService = discountApplicationService;
    }

    #endregion

    #region Nested classes

    protected class ProductPrice
    {
        public double Price { get; set; }
        public double AppliedDiscountAmount { get; set; }
        public IList<ApplyDiscount> AppliedDiscounts { get; set; } = new List<ApplyDiscount>();
        public TierPrice PreferredTierPrice { get; set; }
    }

    #endregion

    #region Fields

    private readonly IContextAccessor _contextAccessor;
    private readonly IProductService _productService;
    private readonly IMediator _mediator;
    private readonly ICurrencyService _currencyService;
    private readonly IDiscountHandlerService _discountApplicationService;
    private readonly ShoppingCartSettings _shoppingCartSettings;
    private readonly CatalogSettings _catalogSettings;

    #endregion

    #region Methods

    /// <summary>
    ///     Gets the final price
    /// </summary>
    /// <param name="product">Product</param>
    /// <param name="customer">The customer</param>
    /// <param name="store">Store</param>
    /// <param name="currency">The currency</param>
    /// <param name="additionalCharge">Additional charge</param>
    /// <param name="includeDiscounts">A value indicating whether include discounts or not for final price computation</param>
    /// <param name="quantity">Shopping cart item quantity</param>
    /// <returns>Final price</returns>
    public virtual async
        Task<(double finalPrice, double discountAmount, List<ApplyDiscount> appliedDiscounts, TierPrice
            preferredTierPrice)>
        GetFinalPrice(
            Product product,
            Customer customer,
            Store store,
            Currency currency,
            double additionalCharge = 0,
            bool includeDiscounts = true,
            int quantity = 1)
    {
        return await GetFinalPrice(product, customer, store, currency, additionalCharge, includeDiscounts, quantity,
            null, null);
    }

    /// <summary>
    ///     Gets the final price
    /// </summary>
    /// <param name="product">Product</param>
    /// <param name="customer">The customer</param>
    /// <param name="store">Store</param>
    /// <param name="currency">Currency</param>
    /// <param name="additionalCharge">Additional charge</param>
    /// <param name="includeDiscounts">A value indicating whether include discounts or not for final price computation</param>
    /// <param name="quantity">Shopping cart item quantity</param>
    /// <param name="rentalStartDate">Rental period start date (for rental products)</param>
    /// <param name="rentalEndDate">Rental period end date (for rental products)</param>
    /// <returns>Final price</returns>
    public virtual async
        Task<(double finalPrice, double discountAmount, List<ApplyDiscount> appliedDiscounts, TierPrice
            preferredTierPrice)>
        GetFinalPrice(
            Product product,
            Customer customer,
            Store store,
            Currency currency,
            double additionalCharge,
            bool includeDiscounts,
            int quantity,
            DateTime? rentalStartDate,
            DateTime? rentalEndDate)
    {
        ArgumentNullException.ThrowIfNull(product);

        double discountAmount = 0;
        var appliedDiscounts = new List<ApplyDiscount>();

        async Task<ProductPrice> PrepareModel()
        {
            var result = new ProductPrice();

            //initial price
            var price =
                product.ProductPrices.FirstOrDefault(x => x.CurrencyCode == currency.CurrencyCode)?.Price ??
                await _currencyService.ConvertFromPrimaryStoreCurrency(product.Price, currency);

            //tier prices
            var tierPrice = product.GetPreferredTierPrice(customer, store.Id, currency.CurrencyCode, quantity);
            if (tierPrice != null)
            {
                price = tierPrice.Price;
                result.PreferredTierPrice = tierPrice;
            }

            //customer product price
            if (_catalogSettings.CustomerProductPrice)
            {
                var customerPrice = await _mediator.Send(new GetPriceByCustomerProductQuery { CustomerId = customer.Id, ProductId = product.Id });
                if (customerPrice.HasValue && customerPrice.Value <
                    await _currencyService.ConvertToPrimaryStoreCurrency(price, currency))
                    price = await _currencyService.ConvertFromPrimaryStoreCurrency(customerPrice.Value, currency);
            }

            //additional charge
            price += additionalCharge;

            //reservations
            if (product.ProductTypeId == ProductType.Reservation)
                if (rentalStartDate.HasValue && rentalEndDate.HasValue)
                {
                    _ = product.IncBothDate
                        ? double.TryParse(
                            ((rentalEndDate - rentalStartDate).Value.TotalDays + 1).ToString(CultureInfo
                                .InvariantCulture), out var d)
                        : double.TryParse(
                            (rentalEndDate - rentalStartDate).Value.TotalDays.ToString(CultureInfo.InvariantCulture),
                            out d);
                    price *= d;
                }

            if (includeDiscounts)
            {
                //discount
                var (tmpAppliedDiscounts, tmpDiscountAmount) = await _discountApplicationService.GetDiscountAmount(product, customer, store, currency, price);
                price -= tmpDiscountAmount;

                if (tmpAppliedDiscounts != null)
                {
                    result.AppliedDiscounts = tmpAppliedDiscounts.ToList();
                    result.AppliedDiscountAmount = tmpDiscountAmount;
                }
            }

            if (price < 0)
                price = 0;

            //rounding
            result.Price = _shoppingCartSettings.RoundPrices ? RoundingHelper.RoundPrice(price, currency) : price;

            return result;
        }

        var modelprice = await PrepareModel();

        if (!includeDiscounts)
            return (modelprice.Price, discountAmount, appliedDiscounts, modelprice.PreferredTierPrice);
        appliedDiscounts = modelprice.AppliedDiscounts.ToList();
        if (appliedDiscounts.Any()) discountAmount = modelprice.AppliedDiscountAmount;

        return (modelprice.Price, discountAmount, appliedDiscounts, modelprice.PreferredTierPrice);
    }


    /// <summary>
    ///     Gets the shopping cart unit price
    /// </summary>
    /// <param name="shoppingCartItem">The shopping cart item</param>
    /// <param name="product">Product</param>
    /// <param name="includeDiscounts">Include discounts or not for price</param>
    /// <returns>Shopping cart unit price (one item)</returns>
    public virtual async Task<(double unitprice, double discountAmount, List<ApplyDiscount> appliedDiscounts)>
        GetUnitPrice(ShoppingCartItem shoppingCartItem,
            Product product, bool includeDiscounts = true)
    {
        ArgumentNullException.ThrowIfNull(shoppingCartItem);

        return await GetUnitPrice(product,
            _contextAccessor.WorkContext.CurrentCustomer,
            _contextAccessor.StoreContext.CurrentStore,
            _contextAccessor.WorkContext.WorkingCurrency,
            shoppingCartItem.ShoppingCartTypeId,
            shoppingCartItem.Quantity,
            shoppingCartItem.Attributes,
            shoppingCartItem.EnteredPrice,
            shoppingCartItem.RentalStartDateUtc,
            shoppingCartItem.RentalEndDateUtc,
            includeDiscounts);
    }

    /// <summary>
    ///     Gets the shopping cart unit price
    /// </summary>
    /// <param name="product">Product</param>
    /// <param name="customer">Customer</param>
    /// <param name="store">Store</param>
    /// <param name="currency">The currency</param>
    /// <param name="shoppingCartType">Shopping cart type</param>
    /// <param name="quantity">Quantity</param>
    /// <param name="attributes">Product attributes</param>
    /// <param name="customerEnteredPrice">Customer entered price</param>
    /// <param name="rentalStartDate">Rental start date</param>
    /// <param name="rentalEndDate">Rental end date</param>
    /// <param name="includeDiscounts">Include discounts or not for price</param>
    /// <returns>Shopping cart unit price</returns>
    public virtual async Task<(double unitprice, double discountAmount, List<ApplyDiscount> appliedDiscounts)>
        GetUnitPrice(
            Product product,
            Customer customer,
            Store store,
            Currency currency,
            ShoppingCartType shoppingCartType,
            int quantity,
            IList<CustomAttribute> attributes,
            double? customerEnteredPrice,
            DateTime? rentalStartDate,
            DateTime? rentalEndDate,
            bool includeDiscounts)
    {
        ArgumentNullException.ThrowIfNull(product);
        ArgumentNullException.ThrowIfNull(customer);

        double discountAmount = 0;
        var appliedDiscounts = new List<ApplyDiscount>();

        double? finalPrice = null;

        if (customerEnteredPrice.HasValue)
            finalPrice = await _currencyService.ConvertFromPrimaryStoreCurrency(customerEnteredPrice.Value, currency);

        if (!finalPrice.HasValue)
        {
            var combination = product.FindProductAttributeCombination(attributes);
            if (combination != null)
            {
                if (combination.OverriddenPrice.HasValue)
                    finalPrice =
                        await _currencyService.ConvertFromPrimaryStoreCurrency(combination.OverriddenPrice.Value,
                            currency);
                if (combination.TierPrices.Any())
                {
                    var storeId = store.Id;
                    var actualTierPrices = combination.TierPrices
                        .Where(x => string.IsNullOrEmpty(x.StoreId) || x.StoreId == storeId)
                        .Where(x => string.IsNullOrEmpty(x.CustomerGroupId) ||
                                    customer.Groups.Contains(x.CustomerGroupId)).ToList();
                    var tierPrice = actualTierPrices.LastOrDefault(price => quantity >= price.Quantity);
                    if (tierPrice != null)
                        finalPrice = await _currencyService.ConvertFromPrimaryStoreCurrency(tierPrice.Price, currency);
                }
            }
        }

        if (!finalPrice.HasValue)
        {
            //summarize price of all attributes
            double attributesTotalPrice = 0;
            if (attributes != null && attributes.Any())
            {
                var attributeValues = product.ParseProductAttributeValues(attributes);
                if (attributeValues != null)
                    foreach (var attributeValue in attributeValues)
                        attributesTotalPrice += await GetProductAttributeValuePriceAdjustment(attributeValue);
                if (product.ProductTypeId == ProductType.BundledProduct)
                    foreach (var item in product.BundleProducts)
                    {
                        var p1 = await _productService.GetProductById(item.ProductId);
                        var bundledProductsAttributeValues = p1?.ParseProductAttributeValues(attributes);
                        if (bundledProductsAttributeValues == null) continue;
                        foreach (var attributeValue in bundledProductsAttributeValues)
                            attributesTotalPrice += item.Quantity *
                                                    await GetProductAttributeValuePriceAdjustment(attributeValue);
                    }
            }

            if (!product.EnteredPrice)
            {
                var qty = 0;
                if (_shoppingCartSettings.GroupTierPrices)
                    qty = customer.ShoppingCartItems
                        .Where(x => x.ProductId == product.Id)
                        .Where(x => x.ShoppingCartTypeId == shoppingCartType)
                        .Sum(x => x.Quantity);
                if (qty == 0)
                    qty = quantity;

                var getfinalPrice = await GetFinalPrice(product,
                    customer,
                    store,
                    currency,
                    attributesTotalPrice,
                    includeDiscounts,
                    qty,
                    rentalStartDate,
                    rentalEndDate);

                finalPrice = getfinalPrice.finalPrice;
                discountAmount = getfinalPrice.discountAmount;
                appliedDiscounts = getfinalPrice.appliedDiscounts;
            }
        }

        finalPrice ??= 0;

        //rounding
        if (_shoppingCartSettings.RoundPrices) finalPrice = RoundingHelper.RoundPrice(finalPrice.Value, currency);
        return (finalPrice.Value, discountAmount, appliedDiscounts);
    }

    /// <summary>
    ///     Gets the shopping cart item sub total
    /// </summary>
    /// <param name="shoppingCartItem">The shopping cart item</param>
    /// <param name="product">Product</param>
    /// <param name="includeDiscounts">Include discounts or not for price</param>
    /// <returns>Shopping cart item sub total</returns>
    public virtual async Task<(double subTotal, double discountAmount, List<ApplyDiscount> appliedDiscounts)>
        GetSubTotal(ShoppingCartItem shoppingCartItem, Product product,
            bool includeDiscounts = true)
    {
        ArgumentNullException.ThrowIfNull(shoppingCartItem);

        double subTotal = 0;
        //unit price
        var getUnitPrice = await GetUnitPrice(shoppingCartItem, product, includeDiscounts);
        var unitPrice = getUnitPrice.unitprice;
        var discountAmount = getUnitPrice.discountAmount;
        var appliedDiscounts = getUnitPrice.appliedDiscounts;

        //discount
        if (appliedDiscounts.Any())
        {

            var discountedQuantity = appliedDiscounts.Max(x => x.MaximumDiscountedQuantity);
            if (appliedDiscounts.Any(x => x.MaximumDiscountedQuantity.HasValue)
                && shoppingCartItem.Quantity > discountedQuantity.Value)
            {
                var discountedSubTotal = unitPrice * discountedQuantity.Value;
                discountAmount *= discountedQuantity.Value;

                var notDiscountedQuantity = shoppingCartItem.Quantity - discountedQuantity.Value;
                var notDiscountedUnitPrice = await GetUnitPrice(shoppingCartItem, product, false);
                var notDiscountedSubTotal = notDiscountedUnitPrice.unitprice * notDiscountedQuantity;

                subTotal = discountedSubTotal + notDiscountedSubTotal;
            }
            else
            {
                //discount is applied to all items (quantity)
                discountAmount *= shoppingCartItem.Quantity;

                subTotal = unitPrice * shoppingCartItem.Quantity;
            }
        }
        else
        {
            subTotal = unitPrice * shoppingCartItem.Quantity;
        }

        return (subTotal, discountAmount, appliedDiscounts);
    }


    /// <summary>
    ///     Gets the product cost (one item)
    /// </summary>
    /// <param name="product">Product</param>
    /// <param name="attributes">Shopping cart item attributes</param>
    /// <returns>Product cost (one item)</returns>
    public virtual async Task<double> GetProductCost(Product product, IList<CustomAttribute> attributes)
    {
        ArgumentNullException.ThrowIfNull(product);

        var cost = product.ProductCost;
        var attributeValues = product.ParseProductAttributeValues(attributes);
        foreach (var attributeValue in attributeValues)
            switch (attributeValue.AttributeValueTypeId)
            {
                case AttributeValueType.Simple:
                    {
                        //simple attribute
                        cost += attributeValue.Cost;
                    }
                    break;
                case AttributeValueType.AssociatedToProduct:
                    {
                        //bundled product
                        var associatedProduct = await _productService.GetProductById(attributeValue.AssociatedProductId);
                        if (associatedProduct != null)
                            cost += associatedProduct.ProductCost * attributeValue.Quantity;
                    }
                    break;
            }

        return cost;
    }

    /// <summary>
    ///     Get a price adjustment of a product attribute value
    /// </summary>
    /// <param name="value">Product attribute value</param>
    /// <returns>Price adjustment</returns>
    public virtual async Task<double> GetProductAttributeValuePriceAdjustment(ProductAttributeValue value)
    {
        ArgumentNullException.ThrowIfNull(value);

        double adjustment = 0;
        switch (value.AttributeValueTypeId)
        {
            case AttributeValueType.Simple:
                {
                    //simple attribute
                    adjustment = value.PriceAdjustment;
                    if (adjustment > 0)
                        adjustment =
                            await _currencyService.ConvertFromPrimaryStoreCurrency(adjustment,
                                _contextAccessor.WorkContext.WorkingCurrency);
                }
                break;
            case AttributeValueType.AssociatedToProduct:
                {
                    //bundled product
                    var associatedProduct = await _productService.GetProductById(value.AssociatedProductId);
                    if (associatedProduct != null)
                        adjustment = (await GetFinalPrice(associatedProduct,
                            _contextAccessor.WorkContext.CurrentCustomer,
                            _contextAccessor.StoreContext.CurrentStore,
                            _contextAccessor.WorkContext.WorkingCurrency,
                            value.PriceAdjustment)).finalPrice * value.Quantity;
                }
                break;
        }

        return adjustment;
    }

    #endregion
}